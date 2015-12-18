using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PerformanceCalculator.Engine;

namespace PerformanceCalculator
{
    public static class PerformanceCalculatorEngine
    {
        public static ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator> Calculate(ForecastMetaData fmd, string path, ObservationMetaData omd, string file, int[] scope, 
                                                                                              bool includeNegObs = false, bool useFixedHours = false, string siteId=null)
        {
            var startTimesInMinutes = FindUsualForecastStartTimePoints(path, fmd);

            var results = new ConcurrentDictionary<UtcDateTime, HourlySkillScoreCalculator>();
            bool isNotPointingToANumber = false;

            // 1. Load observations
            string[] obsLines = File.ReadAllLines(file);
            var obsPowerInFarm = new SortedDictionary<UtcDateTime, double?>();
            string dateTimePattern = string.Empty;
            string prevDateTimePattern = string.Empty;
            if (!obsLines[0].Contains(omd.ObservationSep)) throw new Exception("Observations series does not contain the char: '" + omd.ObservationSep + "' \ras column seperator. Open the document and check what \rdelimiter char is used to seperate the columns \rand specify this char in the Observations Column Seperator field.");
            for (int i=1; i<obsLines.Length; i++) // Skip the heading...
            {
                var line = obsLines[i];
                var cols = line.Split(omd.ObservationSep);
                if (cols.Length > 1)
                {
                    string timeStampStr = cols[omd.ObservationTimeIndex];
                    string valueStr = cols[omd.ObservationValueIndex];

                    UtcDateTime? dateTime = DateTimeUtil.ParseTimeStamp(timeStampStr, prevDateTimePattern, out dateTimePattern);
                    if (dateTime.HasValue)
                    {
                        if (string.IsNullOrWhiteSpace(prevDateTimePattern) && !string.IsNullOrWhiteSpace(dateTimePattern)) prevDateTimePattern = dateTimePattern;

                        double? value = DoubleUtil.ParseDoubleValue(valueStr);
                        if (value.HasValue)
                        {
                            double val = value.Value;
                            if (omd.ObservationUnitType == "MW") val *= 1000;
                            if (val < 0 && !includeNegObs) obsPowerInFarm.Add(dateTime.Value, null);
                            else obsPowerInFarm.Add(dateTime.Value, val);
                        }
                        else
                        {
                            if (i <= 3) isNotPointingToANumber = true;
                            obsPowerInFarm.Add(dateTime.Value, null);
                        }
                    }
                }
            }

            if (isNotPointingToANumber && obsPowerInFarm.Count == 0) throw new Exception("The Observation Column Index Value is not pointing to a column which contains a double value. Change index value!");

            int obsSteps;
            TimeResolutionType? obsResolution = IdentifyResolution(obsPowerInFarm, out obsSteps);

            // 2. Identify Time Resolution of obs series, if less than an hour, make it hourly avg.
            var firstObs = obsPowerInFarm.First().Key;
            var minutes = startTimesInMinutes.Keys.ToArray();
            foreach (var minute in minutes)
            {
                if (firstObs.Minute == minute)
                {
                    SortedDictionary<UtcDateTime, double?> convertedObsPowerInFarm = ConvertTimeSeriesToHourlyResolution(firstObs, obsPowerInFarm, useFixedHours);
                    startTimesInMinutes[minute] = convertedObsPowerInFarm;
                }
                else if (minute > 0)
                {
                    var first = new UtcDateTime(firstObs.Year, firstObs.Month, firstObs.Day, firstObs.Hour, minute);
                    SortedDictionary<UtcDateTime, double?> convertedObsPowerInFarm = ConvertTimeSeriesToHourlyResolution(first, obsPowerInFarm, useFixedHours, minute);
                    startTimesInMinutes[minute] = convertedObsPowerInFarm;
                }
                else
                {
                    var tmp = firstObs.AddHours(1);
                    firstObs = new UtcDateTime(tmp.Year, tmp.Month, tmp.Day, tmp.Hour);
                    SortedDictionary<UtcDateTime, double?> convertedObsPowerInFarm = ConvertTimeSeriesToHourlyResolution(firstObs, obsPowerInFarm, useFixedHours);
                    startTimesInMinutes[minute] = convertedObsPowerInFarm;
                }
            }
            

            // 3. Search forecastPath for forecast documents...
            dateTimePattern = string.Empty;
            prevDateTimePattern = string.Empty;
            FileInfo[] files;
            string filter = "*.csv";
            if (Directory.Exists(path))
            {
                if (!string.IsNullOrEmpty(fmd.ForecastFileFilter)) filter = fmd.ForecastFileFilter;
                var dir = new DirectoryInfo(path);
                files = dir.GetFiles(filter, SearchOption.AllDirectories);
            }
            else files = new FileInfo[]{new FileInfo(path) };

            // If we only have one single forecast file, then this forecast file is usually 99% of the time a long
            // time series with continous time steps, meaning there are no overlaps. Usually, only the unique hours
            // from the original forecasts have been extracted. In these situations, we are interested in comparing
            // the skill of all hours and only use a single skill-bucket. Instead of making this an
            // explicit setting in the view (which it should be ultimately), it is no supporting this case by
            // convention. This should be changed later for flexibility...
            bool continousSerie = files.Length == 1;
            prevDateTimePattern = string.Empty;
            foreach (var fileInfo in files)
            {
                isNotPointingToANumber = false;
                UtcDateTime? firstTimePoint = null;
                UtcDateTime? firstPossibleHour = null;
                SortedDictionary<UtcDateTime, double?> predPowerInFarm;
                var stream = fileInfo.OpenRead();
                using (var reader = new StreamReader(stream))
                {
                    predPowerInFarm = ReadPredictionFile(reader, fmd, includeNegObs, useFixedHours, siteId, prevDateTimePattern, out firstTimePoint, out firstPossibleHour, out isNotPointingToANumber);
                }

                if (isNotPointingToANumber) throw new Exception("The Forecast Column Index Value is not pointing to a column which contains a double value. Change index value!");
                if (predPowerInFarm.Count == 0) continue;
                if (!firstPossibleHour.HasValue) throw new Exception("The first possible forecast hour could not be determined. Please check forecast file: " + fileInfo.FullName);
                if (!firstTimePoint.HasValue) throw new Exception("First time point in forecast file is not specified. Please check forecast file: " + fileInfo.FullName);
                if (obsResolution.HasValue)
                {
                    var firstForecastHour = firstTimePoint.Value;
                    if (useFixedHours) firstForecastHour = firstPossibleHour.Value;
                    var convertedObsPowerInFarm = startTimesInMinutes[firstForecastHour.Minute];
                    var convertedPredPowerInFarm = ConvertTimeSeriesToHourlyResolution(firstForecastHour, predPowerInFarm, useFixedHours, firstForecastHour.Minute);
                    if (convertedPredPowerInFarm.Count > 0)
                    {
                        UtcDateTime first = convertedPredPowerInFarm.Keys.First();
                        var firstTimePointInMonth = new UtcDateTime(first.Year, first.Month, 1);

                        if (continousSerie)
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
                                skillCalculator.AddContinousSerie(enumer, convertedPredPowerInFarm, convertedObsPowerInFarm);
                            }
                        }
                        else
                        {
                            if (!results.ContainsKey(firstTimePointInMonth))
                                results.TryAdd(firstTimePointInMonth, new HourlySkillScoreCalculator(new List<Turbine>(), scope, (int) omd.NormalizationValue));

                            HourlySkillScoreCalculator skillCalculator = results[firstTimePointInMonth];
                            var enumer = new UtcDateTimeEnumerator(first, convertedPredPowerInFarm.Keys.Last(), TimeResolution.Hour);
                            skillCalculator.Add(enumer, convertedPredPowerInFarm, convertedObsPowerInFarm, fmd.OffsetHoursAhead);
                        }
                    }
                }
            }

            

            // 2.1 Print hourly obs series to file for end-user debugging and verification:
            if(useFixedHours) PrintObsPowerInFarmToDebugFile(obsPowerInFarm);

            return results;
        }

        private static SortedDictionary<int, SortedDictionary<UtcDateTime, double?>> FindUsualForecastStartTimePoints(string path, ForecastMetaData fmd)
        {
            var forecastStartInMinutesOverTheHour = new SortedDictionary<int, SortedDictionary<UtcDateTime, double?>>();
            FileInfo[] files;
            string filter = "*.csv";
            if (Directory.Exists(path))
            {
                if (!string.IsNullOrEmpty(fmd.ForecastFileFilter)) filter = fmd.ForecastFileFilter;
                var dir = new DirectoryInfo(path);
                files = dir.GetFiles(filter, SearchOption.AllDirectories).Take(10).ToArray();
            }
            else files = new FileInfo[] { new FileInfo(path) };

            string prevDateTimePattern = string.Empty;
            foreach (var fileInfo in files)
            {
                bool isNotPointingToANumber = false;
                UtcDateTime? firstTimePoint = null;
                UtcDateTime? firstPossibleHour = null;
                SortedDictionary<UtcDateTime, double?> predPowerInFarm;
                var stream = fileInfo.OpenRead();
                using (var reader = new StreamReader(stream))
                {
                    predPowerInFarm = ReadPredictionFile(reader, fmd, true, false, "N/A", prevDateTimePattern, out firstTimePoint, out firstPossibleHour, out isNotPointingToANumber);
                }

                if (predPowerInFarm.Any())
                {
                    int minutes = predPowerInFarm.First().Key.Minute;
                    if(!forecastStartInMinutesOverTheHour.ContainsKey(minutes)) forecastStartInMinutesOverTheHour.Add(minutes, null);
                }
            }

            return forecastStartInMinutesOverTheHour;
        }

        private static SortedDictionary<UtcDateTime, double?> ReadPredictionFile(StreamReader reader, ForecastMetaData fmd, bool includeNegObs, bool useFixedHours, string siteId, string prevDateTimePattern,
            out UtcDateTime? firstTimePoint,
            out UtcDateTime? firstPossibleHour,
            out bool isNotPointingToANumber)
        {
            isNotPointingToANumber = false;
            firstTimePoint = null;
            firstPossibleHour = null;
            int lineNr = -1;
            string dateTimePattern;
            
            var predPowerInFarm = new SortedDictionary<UtcDateTime, double?>();
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

                bool collect = false;
                if (!string.IsNullOrWhiteSpace(siteId))
                {
                    collect = (cols.Length > 2 && cols[1].Equals(siteId, StringComparison.InvariantCultureIgnoreCase));
                    if (!collect && siteId.Equals("N/A", StringComparison.InvariantCultureIgnoreCase)) collect = true;
                }
                else collect = true;

                string timeStampStr = cols[fmd.ForecastTimeIndex];
                string valueStr = cols[fmd.ForecastValueIndex];
                //int timeStepAhead = lineNr + fmd.OffsetHoursAhead;

                // TODO: IF LINE STARTS WITH TEXT, CONTINUE
                if (string.IsNullOrWhiteSpace(timeStampStr) || !char.IsNumber(timeStampStr[0])) continue;

                dateTimePattern = string.Empty;
                UtcDateTime? dateTime = DateTimeUtil.ParseTimeStamp(timeStampStr, prevDateTimePattern, out dateTimePattern);
                if (dateTime.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(prevDateTimePattern) && !string.IsNullOrWhiteSpace(dateTimePattern)) prevDateTimePattern = dateTimePattern;

                    // Hack for calc SWM perf, REMOVE
                    // if (lineNr == 0 && dateTime.Hour== 23) continue;

                    if (lineNr == 0)
                    {
                        firstTimePoint = dateTime;
                        firstPossibleHour = firstTimePoint;
                        if (useFixedHours && dateTime.HasValue)
                        {
                            var firstHour = dateTime.Value.AddHours(1);
                            firstPossibleHour = new UtcDateTime(firstHour.Year, firstHour.Month, firstHour.Day, firstHour.Hour);
                        }
                    }

                    if (collect)
                    {
                        double? value = DoubleUtil.ParseDoubleValue(valueStr);
                        if (value.HasValue)
                        {
                            double val = value.Value;
                            if (fmd.ForecastUnitType == "MW") val *= 1000;
                            if (val < 0 && !includeNegObs)
                            {
                                if(!predPowerInFarm.ContainsKey(dateTime.Value)) predPowerInFarm.Add(dateTime.Value, null);
                            }
                            else
                            {
                                if(!predPowerInFarm.ContainsKey(dateTime.Value)) predPowerInFarm.Add(dateTime.Value, val);
                            }
                        }
                        else
                        {
                            if (lineNr <= 3) isNotPointingToANumber = true;
                            else
                            {
                                if(!predPowerInFarm.ContainsKey(dateTime.Value)) predPowerInFarm.Add(dateTime.Value, null);
                            }
                        }
                    }

                    lineNr++; // We only count lines with valid time stamps
                }
            }

            return predPowerInFarm;
        }

        private static TimeResolutionType? IdentifyResolution(SortedDictionary<UtcDateTime, double?> series, out int steps)
        {
            steps = 0;
            if (series.Count < 2) return null;

            var timePoints = series.Keys.Take(2).ToArray();
            TimeSpan span = timePoints[1].UtcTime - timePoints[0];
            
            var resolution = span.ToTimeResolution(out steps);
            return resolution;
        }

        private static void PrintObsPowerInFarmToDebugFile(SortedDictionary<UtcDateTime, double?> obsPowerInFarm)
        {
            string fileName = "HourlyAvgObservations.csv";
            //fileName = Path.Combine(DataDir, fileName); should be in native directory

            using (var outputFile = new StreamWriter(fileName, false))
            {
                outputFile.WriteLine("TimeStamp [UTC];Production [MW]");
                foreach (var timeAndProd in obsPowerInFarm)
                {
                    string prodStr = "N/A";
                    if (timeAndProd.Value.HasValue) prodStr = (timeAndProd.Value.Value / 1000).ToPrintableFormat();
                    string line = timeAndProd.Key.UtcTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + ";" + prodStr;
                    outputFile.WriteLine(line);
                }
            }
        }

        private static SortedDictionary<UtcDateTime, double?> ConvertTimeSeriesToHourlyResolution(UtcDateTime firstPossibleHour, SortedDictionary<UtcDateTime, double?> series, bool useFixedHours, int minuteOffset=0)
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
            /*if (resolution == TimeResolutionType.Minute && steps == 5)
            {
                var firstTimePoint = timePoints[0];
                if (useFixedHours && (firstTimePoint.Minute > 0 || firstTimePoint.Second > 0))
                {
                    firstTimePoint = firstTimePoint.AddHours(1);
                    firstTimePoint = new UtcDateTime(firstTimePoint.Year, firstTimePoint.Month, firstTimePoint.Day, firstTimePoint.Hour);
                }
                
                var lastPossibleHour = series.Keys.Last();
                if (useFixedHours && (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0))
                {
                    lastPossibleHour = lastPossibleHour.AddHours(-1);
                    lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                }

                return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
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
                if(firstTimePoint > firstPossibleHour) throw new Exception("First time point is greater than first possible hour, this is wrong, first time point must always be lesser than first possible hour.");
                
                if (firstTimePoint.Minute > 0 || firstTimePoint.Second > 0)
                {
                    firstTimePoint = firstTimePoint.AddHours(1);
                    firstTimePoint = new UtcDateTime(firstTimePoint.Year, firstTimePoint.Month, firstTimePoint.Day, firstTimePoint.Hour);
                }
                var lastPossibleHour = series.Keys.Last();
                if (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0)
                {
                    lastPossibleHour = lastPossibleHour.AddHours(-1);
                    lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                }

                return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }
            if (resolution == TimeResolutionType.Minute && steps == 30)
            {
                // darn, typical market resolution, we need to calc hourly avg...
                var firstTimePoint = timePoints[0];
                if (firstTimePoint > firstPossibleHour) throw new Exception("First time point is greater than first possible hour, this is wrong, first time point must always be lesser than first possible hour.");
                if (firstTimePoint.Minute > 0 || firstTimePoint.Second > 0)
                {
                    firstTimePoint = firstTimePoint.AddHours(1);
                    firstTimePoint = new UtcDateTime(firstTimePoint.Year, firstTimePoint.Month, firstTimePoint.Day, firstTimePoint.Hour);
                }
                var lastPossibleHour = series.Keys.Last();
                if (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0)
                {
                    lastPossibleHour = lastPossibleHour.AddHours(-1);
                    lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
                }

                return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            }*/

            var firstTimePoint = timePoints[0];
            if (firstTimePoint > firstPossibleHour) throw new Exception("First time point is greater than first possible hour, this is wrong, first time point must always be lesser than first possible hour.");
            if (useFixedHours && (firstTimePoint.Minute > 0 || firstTimePoint.Second > 0))
            {
                firstTimePoint = firstTimePoint.AddHours(1);
                firstTimePoint = new UtcDateTime(firstTimePoint.Year, firstTimePoint.Month, firstTimePoint.Day, firstTimePoint.Hour);
            }

            var lastPossibleHour = series.Keys.Last();
            if (lastPossibleHour.Minute > 0 || lastPossibleHour.Second > 0)
            {
                lastPossibleHour = lastPossibleHour.AddHours(-1);
                lastPossibleHour = new UtcDateTime(lastPossibleHour.Year, lastPossibleHour.Month, lastPossibleHour.Day, lastPossibleHour.Hour);
            }
            if (!useFixedHours) lastPossibleHour = lastPossibleHour.Subtract(TimeSpan.FromMinutes(minuteOffset));

            return ProductionDataHelper.CalculateMarketResolutionAverage(firstTimePoint, firstPossibleHour, lastPossibleHour, TimeSpan.FromHours(1), span, series);
            //return new SortedDictionary<UtcDateTime, double?>();
        }
    }

    public static class DoubleUtil
    {
        public static double? ParseDoubleValue(string valueStr)
        {
            valueStr = valueStr.Replace(',', '.');
            if (!string.IsNullOrEmpty(valueStr))
            {
                double val;
                if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    return val;
                }
            }
            return null;
        }
    }

    public static class DateTimeUtil
    {
        public static UtcDateTime? ParseTimeStamp(string timeStampStr, string prevDateTimePattern, out string dateTimePattern)
        {
            dateTimePattern = string.Empty;
            if (timeStampStr.Length <= 7) return null;

            if (timeStampStr.EndsWith("a", StringComparison.InvariantCultureIgnoreCase) || timeStampStr.EndsWith("b", StringComparison.InvariantCultureIgnoreCase))
            {
                timeStampStr = timeStampStr.Substring(0, timeStampStr.Length - 1);
            }
            else if (timeStampStr.EndsWith("(a)", StringComparison.InvariantCultureIgnoreCase) || timeStampStr.EndsWith("(b)", StringComparison.InvariantCultureIgnoreCase))
            {
                timeStampStr = timeStampStr.Substring(0, timeStampStr.Length - 3);
            }

            if (!string.IsNullOrEmpty(prevDateTimePattern) && prevDateTimePattern.Length < timeStampStr.Length)
            {
                if (timeStampStr.Contains("  "))
                {
                    var parts = timeStampStr.Split(' ');
                    timeStampStr = parts[0] + " " + parts[parts.Length - 1];
                }
            }
            if (string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);


            //if (string.IsNullOrEmpty(dateTimePattern)) dateTimePattern = UtcDateTime.GetDateTimePattern(timeStampStr);
            if (timeStampStr.EndsWith("(b)")) return null; // skip local time, all time series should be in UTC....
            DateTime timeStamp;
            bool ok = DateTime.TryParseExact(timeStampStr, dateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStamp);
            if (!ok)
            {
                ok = DateTime.TryParseExact(timeStampStr, dateTimePattern.Split(' ')[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStamp);
                if (!ok)
                {
                    ok = DateTime.TryParseExact(timeStampStr, UtcDateTime.GetDateTimePattern(timeStampStr), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timeStamp);
                    if (!ok) throw new Exception(string.Format("Unable to parse time point: {0}, with format: {1}", timeStampStr, dateTimePattern));
                }
            }
            try
            {
                var dateTime = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);
                prevDateTimePattern = dateTimePattern;
                return dateTime;
            }
            catch (Exception)
            {
                try
                {
                    char deliminator = '.';
                    var parts = timeStampStr.Split(deliminator);
                    if (parts.Length == 1)
                    {
                        parts = timeStampStr.Split('/');
                    }
                    var monthStr = parts[0];
                    var dayStr = parts[1];
                    var yearHourMinStr = parts[2];

                    var yearAndHourMin = yearHourMinStr.Split(' ');
                    var yearStr = yearAndHourMin[0];
                    var hourMin = yearAndHourMin[1].Split(':');
                    var hourStr = hourMin[0];
                    var minStr = hourMin[1];

                    int year = int.Parse(yearStr);
                    int month = int.Parse(monthStr);
                    int day = int.Parse(dayStr);
                    int hour = int.Parse(hourStr);
                    int min = int.Parse(minStr);
                    timeStamp = new DateTime(year, month, day, hour, min, 0, DateTimeKind.Unspecified);
                    UtcDateTime time = UtcDateTime.CreateFromUnspecifiedDateTime(timeStamp);
                    return time;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}