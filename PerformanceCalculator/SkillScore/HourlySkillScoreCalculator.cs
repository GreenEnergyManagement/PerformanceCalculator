using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerformanceCalculator
{
    public class HourlySkillScoreCalculator
    {
        private const int Any = -1; // registering every hour calculated into this slot...
        private readonly List<Turbine> turbines;
        public readonly int[] Scope;
        public readonly int InstalledCapacity;
        private readonly int forecastTimePoints;
        private int nrOfRegisteredForecastTimePoints = 0;
        private readonly SortedDictionary<int, BinSkillScore> skillScoreHourBins = new SortedDictionary<int, BinSkillScore>();
        public readonly SortedDictionary<UtcDateTime, SortedDictionary<UtcDateTime, RestrictedRangeInt>> Success = new SortedDictionary<UtcDateTime, SortedDictionary<UtcDateTime, RestrictedRangeInt>>();
        private bool useObservedWeatherData;

        public HourlySkillScoreCalculator(List<Turbine> turbines, int[] scope, int installedCapacity, int forecastTimePoints = -1, bool useObservedWeatherData = true)
        {
            this.turbines = turbines;
            Scope = scope;
            InstalledCapacity = installedCapacity;
            this.forecastTimePoints = forecastTimePoints;
            this.useObservedWeatherData = useObservedWeatherData;
            foreach (int hour in scope)
            {
                BinSkillScore hourBin;
                if (hour == Any) hourBin = new BinSkillScore(hour, "All", installedCapacity);
                else hourBin = new BinSkillScore(hour, "Hour_" + hour, installedCapacity);

                skillScoreHourBins.Add(hour, hourBin);
            }
        }

        public static int[] GetDefaultScope()
        {
            // -1 is the slot given to any hour, hour zero ahead is the current hour of the forecast, usually cannot be used for forecasting. The rest should be self explanatory.
            return new[] { -1, 0, 1, 2, 3, 4, 5, 6, 12, 18, 24, 36, 48 };
        }

        public void Add(UtcDateTimeEnumerator enumer,
            SortedDictionary<UtcDateTime, double> sumPowerInParkAtTimeTAtTimeT,
            SortedDictionary<UtcDateTime, double> sumObsPowerInParkAtTimeT,
            int hoursAheadOffset)
        {
            nrOfRegisteredForecastTimePoints++;
            int index = hoursAheadOffset;
            foreach (UtcDateTime time in enumer)
            {
                if (sumPowerInParkAtTimeTAtTimeT.ContainsKey(time) && sumObsPowerInParkAtTimeT.ContainsKey(time))
                {
                    if (skillScoreHourBins.ContainsKey(Any)) skillScoreHourBins[Any].Register(sumObsPowerInParkAtTimeT[time], sumPowerInParkAtTimeTAtTimeT[time]);

                    int hourAhead = index;
                    if (skillScoreHourBins.ContainsKey(hourAhead))
                    {
                        skillScoreHourBins[hourAhead].Register(sumObsPowerInParkAtTimeT[time], sumPowerInParkAtTimeTAtTimeT[time]);
                    }
                }
                index++;
            }
        }

        public void Add(UtcDateTimeEnumerator enumer,
            SortedDictionary<UtcDateTime, SortedDictionary<int, double>> powerInParkAtTimeT,
            SortedDictionary<UtcDateTime, SortedDictionary<int, double>> obsPowerInParkAtTimeT,
            SortedDictionary<UtcDateTime, double?> obsMastWeather)
        {
            nrOfRegisteredForecastTimePoints++;
            var sumPowerInParkAtTimeTAtTimeT = new SortedDictionary<UtcDateTime, double>();
            var sumObsPowerInParkAtTimeT = new SortedDictionary<UtcDateTime, double>();
            UtcDateTime forecastStart = enumer.First();
            var forecastSuccess = new SortedDictionary<UtcDateTime, RestrictedRangeInt>();
            if (!Success.ContainsKey(forecastStart)) Success.Add(forecastStart, forecastSuccess);
            double prevRotSpeed = -1;
            int timeIndex = 0;
            foreach (UtcDateTime currentTime in enumer)
            {
                forecastSuccess.Add(currentTime, new RestrictedRangeInt(0, turbines.Count(), 0));
                if (timeIndex > 10 && sumObsPowerInParkAtTimeT.Count == 0)
                {
                    break; // If we have nothing for the ten first hours then this forecast point is broken and should not be taken into account...
                }

                if (useObservedWeatherData)
                {
                    if (obsMastWeather.Count == 0) throw new Exception("No MetMast Data provided, calculating skill score without met mast data must be implemented.");
                }

                bool isOkData = true;
                int nrOfTubrines = 0;
                if (useObservedWeatherData && obsMastWeather.ContainsKey(currentTime) && obsMastWeather[currentTime].HasValue && (obsMastWeather[currentTime].Value - prevRotSpeed).AboutEqual(0.0)) isOkData = false;

                if (isOkData)
                {
                    if (useObservedWeatherData && obsMastWeather.ContainsKey(currentTime) && obsMastWeather[currentTime].HasValue) prevRotSpeed = obsMastWeather[currentTime].Value;

                    if (powerInParkAtTimeT.ContainsKey(currentTime))
                    {
                        foreach (Turbine turbine in turbines)
                        {
                            if (obsPowerInParkAtTimeT.ContainsKey(currentTime) && obsPowerInParkAtTimeT[currentTime].ContainsKey(turbine.ScadaId))
                            {
                                double power = 0;
                                if (powerInParkAtTimeT.ContainsKey(currentTime) && powerInParkAtTimeT[currentTime].ContainsKey(turbine.ReferenceId)) power = powerInParkAtTimeT[currentTime][turbine.ReferenceId];
                                double obsPower = obsPowerInParkAtTimeT[currentTime][turbine.ScadaId];

                                if (!sumPowerInParkAtTimeTAtTimeT.ContainsKey(currentTime)) sumPowerInParkAtTimeTAtTimeT.Add(currentTime, 0);
                                sumPowerInParkAtTimeTAtTimeT[currentTime] += power;

                                if (!sumObsPowerInParkAtTimeT.ContainsKey(currentTime)) sumObsPowerInParkAtTimeT.Add(currentTime, 0);
                                sumObsPowerInParkAtTimeT[currentTime] += obsPower;

                                nrOfTubrines++;
                            }
                        }
                    }
                }
                forecastSuccess[currentTime].Value = nrOfTubrines;

                timeIndex++;
            }

            int index = 0;
            foreach (UtcDateTime time in enumer)
            {
                if (sumPowerInParkAtTimeTAtTimeT.ContainsKey(time) && sumObsPowerInParkAtTimeT.ContainsKey(time))
                {
                    if (skillScoreHourBins.ContainsKey(Any)) skillScoreHourBins[Any].Register(sumObsPowerInParkAtTimeT[time], sumPowerInParkAtTimeTAtTimeT[time]);

                    int hourAhead = index;
                    if (skillScoreHourBins.ContainsKey(hourAhead))
                    {
                        skillScoreHourBins[hourAhead].Register(sumObsPowerInParkAtTimeT[time], sumPowerInParkAtTimeTAtTimeT[time]);
                    }
                }
                index++;
            }
        }

        public void AddContinousSerie(UtcDateTimeEnumerator enumer, SortedDictionary<UtcDateTime, double> predictionFrames, SortedDictionary<UtcDateTime, double> productionFrames)
        {
            Dictionary<int, int> hourScope = Scope.ToDictionary(e => e, e => e);
            nrOfRegisteredForecastTimePoints++;
            double sumPower = 0;
            double sumObsPower = 0;

            double prevPower = -1;
            int timeIndex = 0;
            int regIndex = 0;
            foreach (UtcDateTime currentTime in enumer)
            {
                if (timeIndex > 10 && sumObsPower <= 1)
                {
                    break; // If we have nothing for the ten first hours then this forecast point is broken and should not be taken into account...
                }

                if (predictionFrames.ContainsKey(currentTime) && productionFrames.ContainsKey(currentTime) && !predictionFrames[currentTime].AboutEqual(prevPower))
                {
                    double power = predictionFrames[currentTime];
                    double obsPower = productionFrames[currentTime];

                    sumPower += power;
                    sumObsPower += obsPower;

                    if(skillScoreHourBins.ContainsKey(Any)) skillScoreHourBins[Any].Register(obsPower, power);

                    int currentHour = currentTime.UtcTime.Hour;
                    if (hourScope.ContainsKey(currentHour))
                    {
                        int scopeHour = hourScope[currentHour];
                        if (skillScoreHourBins.ContainsKey(scopeHour))
                        {
                            skillScoreHourBins[scopeHour].Register(obsPower, power);
                            regIndex++;
                        }
                    }

                    prevPower = obsPower;
                }

                timeIndex++;
            }

            var first = enumer.First();
            var suc = new SortedDictionary<UtcDateTime, RestrictedRangeInt>()
            {
                {first, new RestrictedRangeInt(0, timeIndex, regIndex)}
            };
            Success.Add(first, suc);
        }

        public Percent GetAvgSuccessfullyCorrected()
        {
            double avg = Success.Average(e => e.Value.Average(f => f.Value.Value));
            var avgCorr = new RestrictedRangeDouble(0, turbines.Count(), avg);
            return avgCorr.ToPercent();
        }

        public Percent GetPeriodProcessedPercentage()
        {
            double percent = (((double)nrOfRegisteredForecastTimePoints) / ((double)forecastTimePoints)) * 100.0;
            return new Percent(percent);
        }

        public string ToStringAggregate(List<BinSkillScore> skills)
        {
            return skills.Aggregate("", (acc, skill) => acc + "\n" + skill);
        }

        public string WriteReport(List<BinSkillScore> skills, string heading = null)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(heading)) builder.AppendLine(heading);
            builder.AppendLine(ToStringAggregate(skills));

            double avg = Success.Average(e => e.Value.Average(f => f.Value.Value));
            var avgCorr = new RestrictedRangeDouble(0, turbines.Count(), avg);
            builder.AppendLine("Avg % Compared: " + GetAvgSuccessfullyCorrected());
            builder.AppendLine(string.Format("Able to process {0}% of the weather forecasts", GetPeriodProcessedPercentage()));
            builder.AppendLine("For inspection of all time points, check the success property.");

            return builder.ToString();
        }

        public string WriteReport(Dictionary<int, BinSkillScore> hourlySkills, string heading = null)
        {
            return WriteReport(hourlySkills.Values.ToList(), heading);
        }

        public string WriteReport(string heading = null)
        {
            return WriteReport(skillScoreHourBins.Values.ToList(), heading);
        }

        public IEnumerable<BinSkillScore> GetSkillScoreBins()
        {
            return skillScoreHourBins.Values;
        }

        public SortedDictionary<int, BinSkillScore> GetSkillScoreBinsDictionary()
        {
            return skillScoreHourBins;
        }
    }
}