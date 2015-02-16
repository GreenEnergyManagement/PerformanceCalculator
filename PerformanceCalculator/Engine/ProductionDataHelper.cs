using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceCalculator.Engine
{
    public static class ProductionDataHelper
    {
        internal static SortedDictionary<UtcDateTime, double?> CalculateMarketResolutionAverage(UtcDateTime firstTimePoint, UtcDateTime first, UtcDateTime last, TimeSpan marketResolution, TimeSpan dataResolution, SortedDictionary<UtcDateTime, double?> prod, bool isForecast=false)
        {
            int steps;
            TimeResolutionType timeResolutionType = dataResolution.ToTimeResolution(out steps);

            // Create TimePoints that we are interested to aggregate data on:
            var time = last;//.Subtract(marketResolution);
            var timePoints = new List<UtcDateTime> { last };
            do
            {
                time = time.Subtract(marketResolution);
                if (time >= first) timePoints.Add(time);
            } while (time >= first);

            var resolutionSamples = new SortedDictionary<UtcDateTime, List<double?>>();
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
                            double? production = prod[utcDateTime];
                            if (!resolutionSamples.ContainsKey(point)) resolutionSamples.Add(point, new List<double?>());
                            resolutionSamples[point].Add(production);
                        }
                    }
                }
                else // This is for the last point...
                {
                    UtcDateTime point = timePoints[index]; // this should be end time e.g 12:00
                    UtcDateTime prevPoint = timePoints[index].Subtract(marketResolution).Add(dataResolution); // this should be time resolution before, e.g. an hour, thus 11:00, but maybe adding data resolution e.g. 10.
                    var enumer = new UtcDateTimeEnumerator(prevPoint, point, timeResolutionType, steps);
                    foreach (UtcDateTime utcDateTime in enumer)
                    {
                        if (prod.ContainsKey(utcDateTime))
                        {
                            double? production = prod[utcDateTime];
                            if (!resolutionSamples.ContainsKey(point)) resolutionSamples.Add(point, new List<double?>());
                            resolutionSamples[point].Add(production);
                        }
                    }
                }
            }

            int productionStepsInResolution = CalculateProductionStepsInResolution(marketResolution, dataResolution);
            var avgProds = new SortedDictionary<UtcDateTime, double?>();

            foreach (UtcDateTime dateTime in resolutionSamples.Keys)
            {
                var prodInResolution = resolutionSamples[dateTime];
                // When calculating observed production, we always calculate the average as sum devided by time steps in period (resolution)
                double? avgPower;
                if (!isForecast) avgPower = prodInResolution.Sum()/productionStepsInResolution;
                else
                {   // When calculating predicted production, we always calculate the pure average
                    double sum = 0;
                    int nrOfObsInPeriod = 0;
                    foreach (double? p in prodInResolution)
                    {
                        if (p.HasValue)
                        {
                            nrOfObsInPeriod++;
                            sum += p.Value;
                        }
                    }

                    if (nrOfObsInPeriod > 0) avgPower = sum/nrOfObsInPeriod;
                    else avgPower = null;
                }
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