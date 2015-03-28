using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using PerformanceCalculator.Engine;

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
            var obsPowerInFarm = new SortedDictionary<UtcDateTime, double?>();
            string dateTimePattern = string.Empty;
            if (!obsLines[0].Contains(omd.ObservationSep)) throw new Exception("Observations series does not contain the char: '" + omd.ObservationSep + "' \ras column seperator. Open the document and check what \rdelimiter char is used to seperate the columns \rand specify this char in the Observations Column Seperator field.");
            for (int i=1; i<obsLines.Length; i++) // Skip the heading...
            {
                var line = obsLines[i];
                var cols = line.Split(omd.ObservationSep);
                if (cols.Length > 1)
                {
                    string timeStampStr = cols[omd.ObservationTimeIndex];
                    string valueStr = cols[omd.ObservationValueIndex];

                    //01.05.2014 13:00
                    if (string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);
                    if (timeStampStr.EndsWith("(b)")) continue; // skip local time, all time series should be in UTC....
                    DateTime timeStamp = DateTime.ParseExact(timeStampStr, dateTimePattern, (IFormatProvider) CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    var dateTime = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);

                    valueStr = valueStr.Replace(',', '.');
                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        double val;
                        if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                        {
                            if (omd.ObservationUnitType == "MW") val *= 1000;
                            obsPowerInFarm.Add(dateTime, val);
                        }
                        else
                        {
                            if(i <= 3) isNotPointingToANumber = true;
                            else obsPowerInFarm.Add(dateTime, null);
                        }
                    }
                    else obsPowerInFarm.Add(dateTime, null);
                }
            }

            if (isNotPointingToANumber && obsPowerInFarm.Count == 0) throw new Exception("The Observation Column Index Value is not pointing to a column which contains a double value. Change index value!");
            
            
            // 2. Identify Time Resolution of obs series, if less than an hour, make it hourly avg.
            obsPowerInFarm = ConvertTimeSeriesToHourlyResolution(obsPowerInFarm.First().Key, obsPowerInFarm);


            // 3. Search forecastPath for forecast documents...
            dateTimePattern = string.Empty;
            string filter = "*.csv";
            if (!string.IsNullOrEmpty(fmd.ForecastFileFilter)) filter = fmd.ForecastFileFilter;
            FileInfo[] files = path.GetFiles(filter, SearchOption.AllDirectories);

            // If we only have one single forecast file, then this forecast file is usually 99% of the time a long
            // time series with continous time steps, meaning there are no overlaps. Usually, only the unique hours
            // from the original forecasts have been extracted. In these situations, we are interested in comparing
            // the skill of forecasts at every 12:00 or 14:00 hour regardless of date. Instead of making this an
            // explicit setting in the view (which it should be ultimately), it is no supporting this case by
            // convention. This should be changed later for flexibility...
            bool compareHoursInDay = files.Length == 1;
            foreach (var fileInfo in files)
            {
                isNotPointingToANumber = false;
                UtcDateTime? firstTimePoint = null;
                UtcDateTime? firstPossibleHour = null;
                var predPowerInFarm = new SortedDictionary<UtcDateTime, double?>();
                var stream = fileInfo.OpenRead();
                var reader = new StreamReader(stream);
                int lineNr = -1;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (lineNr == -1)
                    {
                        if (!line.Contains(fmd.ForecastSep))
                            throw new Exception("Forecast series does not contain the char: '" + fmd.ForecastSep +
                                                "' \ras column seperator. Open a document and check what \rdelimiter char is used to seperate the columns \rand specify this char in the Forecasts Column Seperator field.");
                        lineNr++;
                        continue;
                    }
                    if (string.IsNullOrEmpty(line)) continue;

                    var cols = line.Split(fmd.ForecastSep);
                    string timeStampStr = cols[fmd.ForecastTimeIndex];
                    string valueStr = cols[fmd.ForecastValueIndex];
                    //int timeStepAhead = lineNr + fmd.OffsetHoursAhead;

                    if (string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);
                    if (timeStampStr.EndsWith("(b)")) continue; // skip local time, all time series should be in UTC....
                    DateTime timeStamp;
                    bool ok = DateTime.TryParseExact(timeStampStr, dateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStamp);
                    if (!ok)
                    {
                        ok = DateTime.TryParseExact(timeStampStr, dateTimePattern.Split(' ')[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStamp);
                        if (!ok) throw new Exception(string.Format("Unable to parse time point: {0}, with format: {1}", timeStampStr, dateTimePattern.Split(' ')[0]));
                    }
                    var dateTime = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);

                    if (lineNr == 0)
                    {
                        firstTimePoint = dateTime;
                        var firstHour = dateTime.AddHours(1);
                        firstPossibleHour = new UtcDateTime(firstHour.Year, firstHour.Month, firstHour.Day, firstHour.Hour);
                    }

                    double val;
                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                        {
                            if (fmd.ForecastUnitType == "MW") val *= 1000;
                            predPowerInFarm.Add(dateTime, val);
                        }
                        else
                        {
                            if (lineNr <= 3) isNotPointingToANumber = true;
                            else predPowerInFarm.Add(dateTime, null);
                        }
                    }
                    else predPowerInFarm.Add(dateTime, null);
                    lineNr++;
                }

                if (isNotPointingToANumber && predPowerInFarm.Count == 0)
                    throw new Exception("The Forecast Column Index Value is not pointing to a column which contains a double value. Change index value!");
                if (!firstPossibleHour.HasValue) throw new Exception("The first possible forecast hour could not be determined. Please check forecast file: " + fileInfo.FullName);
                if (!firstTimePoint.HasValue) throw new Exception("First time point in forecast file is not specified. Please check forecast file: " + fileInfo.FullName);
                else
                {
                    var convertedPredPowerInFarm = ConvertTimeSeriesToHourlyResolution(firstPossibleHour.Value, predPowerInFarm);

                    if (convertedPredPowerInFarm.Count > 0)
                    {
                        UtcDateTime first = convertedPredPowerInFarm.Keys.First();
                        var firstTimePointInMonth = new UtcDateTime(first.Year, first.Month, 1);

                        if (compareHoursInDay)
                        {
                            // Split up into months
                            UtcDateTime last = convertedPredPowerInFarm.Keys.Last();
                            var firstTimePointInLastMonth = new UtcDateTime(last.Year, last.Month, 1);
                            for (var time = firstTimePointInMonth; time.UtcTime <= firstTimePointInLastMonth.UtcTime; time = new UtcDateTime(time.UtcTime.AddMonths(1)))
                            {
                                if (!results.ContainsKey(time))
                                    results.TryAdd(time, new HourlySkillScoreCalculator(new List<Turbine>(), scope, (int) omd.NormalizationValue));

                                HourlySkillScoreCalculator skillCalculator = results[time];
                                var enumer = new UtcDateTimeEnumerator(time, new UtcDateTime(time.UtcTime.AddMonths(1)), TimeResolution.Hour);
                                skillCalculator.AddContinousSerie(enumer, convertedPredPowerInFarm, obsPowerInFarm);
                            }
                        }
                        else
                        {
                            if (!results.ContainsKey(firstTimePointInMonth))
                                results.TryAdd(firstTimePointInMonth, new HourlySkillScoreCalculator(new List<Turbine>(), scope, (int) omd.NormalizationValue));

                            HourlySkillScoreCalculator skillCalculator = results[firstTimePointInMonth];
                            var enumer = new UtcDateTimeEnumerator(first, convertedPredPowerInFarm.Keys.Last(), TimeResolution.Hour);
                            skillCalculator.Add(enumer, convertedPredPowerInFarm, obsPowerInFarm, fmd.OffsetHoursAhead);
                        }
                    }
                }
            }

            return results;
        }

        private static SortedDictionary<UtcDateTime, double?> ConvertTimeSeriesToHourlyResolution(UtcDateTime firstPossibleHour, SortedDictionary<UtcDateTime, double?> series)
        {
            if(series.Count==0) return new SortedDictionary<UtcDateTime, double?>();

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
                var lastPossibleHour = series.Keys.Last();
                lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                
                return ProductionDataHelper.CalculateMarketResolutionAverage(timePoints[0], firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }
            if (resolution == TimeResolutionType.Minute && steps == 15)
            {
                // darn, typical market resolution, we need to calc hourly avg...
                var firstTimePoint = timePoints[0];
                if(firstTimePoint > firstPossibleHour) throw new Exception("First time point is greater than first possible hour, this is wrong, first time point must always be greater than first possible hour.");
                var lastPossibleHour = series.Keys.Last();
                if (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0)
                {
                    lastPossibleHour = lastPossibleHour.AddHours(-1);
                    lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                }
                /*var enumer = new UtcDateTimeEnumerator(firstPossibleHour, lastPossibleHour, TimeResolution.Hour);
                foreach (var time in enumer)
                {
                    // Should always contain time if time series is in UTC. If not, then you are doing something we do not recommend.
                    if (series.ContainsKey(time)) Console.WriteLine(time.UtcTime.ToString(dateTimePattern, CultureInfo.InvariantCulture) + ";" + series[time]); 
                }*/

                return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }
            if (resolution == TimeResolutionType.Minute && steps == 30)
            {
                // darn, typical market resolution, we need to calc hourly avg...
                var firstTimePoint = timePoints[0];
                if (firstTimePoint > firstPossibleHour) throw new Exception("First time point is greater than first possible hour, this is wrong, first time point must always be greater than first possible hour.");
                var lastPossibleHour = series.Keys.Last();
                if (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0)
                {
                    lastPossibleHour = lastPossibleHour.AddHours(-1);
                    lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                }
                /*var enumer = new UtcDateTimeEnumerator(firstPossibleHour, lastPossibleHour, TimeResolution.Hour);
                foreach (var time in enumer)
                {
                    // Should always contain time if time series is in UTC. If not, then you are doing something we do not recommend.
                    if (series.ContainsKey(time)) Console.WriteLine(time.UtcTime.ToString(dateTimePattern, CultureInfo.InvariantCulture) + ";" + series[time]); 
                }*/

                return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }
            return new SortedDictionary<UtcDateTime, double?>();
        }
    }
}