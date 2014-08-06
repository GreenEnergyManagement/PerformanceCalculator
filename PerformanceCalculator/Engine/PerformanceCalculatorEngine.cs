using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PerformanceCalculator
{
    public static class PerformanceCalculatorEngine
    {
        public static ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator> Calculate(ForecastMetaData fmd, DirectoryInfo path, ObservationMetaData omd, string file, int[] scope)
        {
            var results = new ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator>();
            bool isNotPointingToANumber = false;

            // 1. Load observations
            string[] obsLines = File.ReadAllLines(file);
            var obsPowerInFarm = new SortedDictionary<UtcDateTime, double>();
            string dateTimePattern = string.Empty;
            foreach (var line in obsLines.Skip(1)) // Skip the heading...
            {
                if(!line.Contains(omd.ObservationSep)) throw new Exception("Observations series does not contain the char: '"+omd.ObservationSep+"' \ras column seperator. Open the document and check what \rdelimiter char is used to seperate the columns \rand specify this char in the Observations Column Seperator field.");
                var cols = line.Split(omd.ObservationSep);
                string timeStampStr = cols[omd.ObservationTimeIndex];
                string valueStr = cols[omd.ObservationValueIndex];

                //01.05.2014 13:00
                if(string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);
                DateTime timeStamp = DateTime.ParseExact(timeStampStr, dateTimePattern, (IFormatProvider)CultureInfo.InvariantCulture, DateTimeStyles.None);
                var dateTime = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);

                valueStr = valueStr.Replace(',', '.');
                double val;
                if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    if (omd.ObservationUnitType == "MW") val *= 1000;
                    obsPowerInFarm.Add(dateTime, val);
                }
                else isNotPointingToANumber = true;
            }

            if (isNotPointingToANumber && obsPowerInFarm.Count == 0) throw new Exception("The Observation Column Index Value is not pointing to a column which contains a double value. Change index value!");
            // 2. Identify Time Resolution of obs series, if less than an hour, make it hourly avg.
            obsPowerInFarm = ConvertTimeSeriesToHourlyResolution(obsPowerInFarm, dateTimePattern);

            // 3. Search forecastPath for forecast documents...
            dateTimePattern = string.Empty;
            FileInfo[] files = path.GetFiles("*.csv", SearchOption.AllDirectories);
            foreach (var fileInfo in files)
            {
                isNotPointingToANumber = false;
                var predPowerInFarm = new SortedDictionary<UtcDateTime, double>();
                var stream = fileInfo.OpenRead();
                var reader = new StreamReader(stream);
                int lineNr = -1;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (lineNr == -1)
                    {
                        if (!line.Contains(fmd.ForecastSep)) throw new Exception("Forecast series does not contain the char: '" + fmd.ForecastSep + "' \ras column seperator. Open a document and check what \rdelimiter char is used to seperate the columns \rand specify this char in the Forecasts Column Seperator field.");
                        lineNr++;
                        continue;
                    }

                    var cols = line.Split(fmd.ForecastSep);
                    string timeStampStr = cols[fmd.ForecastTimeIndex];
                    string valueStr = cols[fmd.ForecastValueIndex];
                    //int timeStepAhead = lineNr + fmd.OffsetHoursAhead;

                    if (string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);
                    DateTime timeStamp = DateTime.ParseExact(timeStampStr, dateTimePattern, (IFormatProvider)CultureInfo.InvariantCulture, DateTimeStyles.None);
                    var dateTime = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);

                    double val;
                    if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    {
                        if (fmd.ForecastUnitType == "MW") val *= 1000;
                        predPowerInFarm.Add(dateTime, val);
                    }
                    else isNotPointingToANumber = true;

                    lineNr++;
                }

                if (isNotPointingToANumber && predPowerInFarm.Count==0) throw new Exception("The Forecast Column Index Value is not pointing to a column which contains a double value. Change index value!");
                predPowerInFarm = ConvertTimeSeriesToHourlyResolution(predPowerInFarm, dateTimePattern);

                UtcDateTime first = predPowerInFarm.Keys.First();
                var firstTimePointInMonth = new UtcDateTime(first.Year, first.Month, 1);
                if (!results.ContainsKey(firstTimePointInMonth))
                    results.TryAdd(firstTimePointInMonth, new HourlySkillScoreCalculator(new List<Turbine>(), scope, (int)omd.NormalizationValue));

                HourlySkillScoreCalculator skillCalculator = results[firstTimePointInMonth];
                var enumer = new UtcDateTimeEnumerator(first, predPowerInFarm.Keys.Last(), TimeResolution.Hour);
                skillCalculator.Add(enumer, predPowerInFarm, obsPowerInFarm, fmd.OffsetHoursAhead);
            }

            /*Parallel.ForEach(times, t =>
            {
                var to = t.AddMonths(1);
                HourlySkillScoreCalculator calc = DoTestPerformanceOnlinePowerCorrOnCalculatePowerForAllTurbinesWind(new UtcDateTime(t), new UtcDateTime(to), cfg, scope, ofWcTurbines, onWcTurbines, ofPcTurbines, onPcTurbines,
                                                                                farm, weatherCorrection, stabHandler, selector, wakeCalculator, powerCorrection, cache, configCache,
                                                                                mergeSettingsWeather: mergeSettingsWeather, mergeSettingsPower: mergeSettingsPower);
                results.TryAdd(t, calc);
            });*/

            return results;
        }

        private static SortedDictionary<UtcDateTime, double> ConvertTimeSeriesToHourlyResolution(SortedDictionary<UtcDateTime, double> series, string dateTimePattern)
        {
            var timePoints = series.Keys.Take(2).ToArray();
            TimeSpan span = timePoints[1].UtcTime - timePoints[0];
            int steps;
            var resolution = span.ToTimeResolution(out steps);

            if (resolution == TimeResolutionType.Hour && steps == 1)
            {
                /* Great, do nothing...*/
                return series;
            }
            if (resolution == TimeResolutionType.Minute && steps == 10)
            {
                // darn, typical scada resolution, we need to calc hourly avg...
                var firstPossibleHour = timePoints[0].AddHours(1);
                var lastPossibleHour = series.Keys.Last();
                lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);

                /*var enumer = new UtcDateTimeEnumerator(timePoints[0], lastPossibleHour, TimeResolution.Hour);
                foreach (var time in enumer)
                {
                    Console.WriteLine(time.UtcTime.ToString(dateTimePattern, CultureInfo.InvariantCulture) + ";" + series[time]); 
                }*/
                
                return ProductionDataHelper.CalculateMarketResolutionAverage(firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }
            return new SortedDictionary<UtcDateTime, double>();
        }
    }

    public static class ProductionDataHelper
    {
        internal static SortedDictionary<UtcDateTime, double> CalculateMarketResolutionAverage(
            UtcDateTime first, UtcDateTime last,
            TimeSpan marketResolution, TimeSpan dataResolution,
            SortedDictionary<UtcDateTime, double> prod)
        {
            int steps;
            TimeResolutionType timeResolutionType = dataResolution.ToTimeResolution(out steps);

            // Create TimePoints that we are interested to aggregate data on:
            var time = last.Subtract(marketResolution);
            var timePoints = new List<UtcDateTime> { last, time };
            do
            {
                time = time.Subtract(marketResolution);
                if (time >= first) timePoints.Add(time);
            } while (time >= first);

            var resolutionSamples = new SortedDictionary<UtcDateTime, List<double>>();
            for (int index = 0; index < timePoints.Count; index++)
            {
                if (index + 1 < timePoints.Count)
                {
                    UtcDateTime point = timePoints[index]; // this should be end time e.g 12:00
                    UtcDateTime prevPoint = timePoints[index + 1].Add(dataResolution); // this should be time resolution before, e.g. an hour, thus 11:00, but maybe adding data resolution e.g. 10.

                    var enumer = new UtcDateTimeEnumerator(prevPoint, point, timeResolutionType, steps);
                    foreach (UtcDateTime utcDateTime in enumer)
                    {
                        if (prod.ContainsKey(utcDateTime))
                        {
                            double production = prod[utcDateTime];
                            if (!resolutionSamples.ContainsKey(point)) resolutionSamples.Add(point, new List<double>());
                            resolutionSamples[point].Add(production);
                        }
                    }
                }
            }

            int productionStepsInResolution = CalculateProductionStepsInResolution(marketResolution, dataResolution);
            var avgProds = new SortedDictionary<UtcDateTime, double>();

            foreach (UtcDateTime dateTime in resolutionSamples.Keys)
            {
                var prodInResolution = resolutionSamples[dateTime];
                double avgPower = prodInResolution.Sum() / productionStepsInResolution;
                avgProds.Add(dateTime, avgPower);
            }
            return avgProds;
        }

        private static int CalculateProductionStepsInResolution(TimeSpan resolution, TimeSpan dataResolution)
        {
            double productionStepsInResolutionD = resolution.TotalMinutes / dataResolution.TotalMinutes;
            int productionStepsInResolution = (int)productionStepsInResolutionD;
            double fraction = productionStepsInResolutionD - productionStepsInResolution;

            if (fraction.AboutEqual(0.0)) return productionStepsInResolution;

            if (fraction.AboutEqual(0.5)) productionStepsInResolution = 2;
            else productionStepsInResolution = (int)dataResolution.TotalMinutes;

            return productionStepsInResolution;
        }
    }
}