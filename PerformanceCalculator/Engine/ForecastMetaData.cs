namespace PerformanceCalculator
{
    public class ForecastMetaData
    {
        public readonly int ForecastTimeIndex;
        public readonly int ForecastValueIndex;
        public readonly int OffsetHoursAhead;
        public readonly char ForecastSep;
        public readonly string ForecastUnitType;

        public ForecastMetaData(int forecastTimeIndex, int forecastValueIndex, int offsetHoursAhead, char forecastSep, string forecastUnitType)
        {
            ForecastTimeIndex = forecastTimeIndex;
            ForecastValueIndex = forecastValueIndex;
            OffsetHoursAhead = offsetHoursAhead;
            ForecastSep = forecastSep;
            ForecastUnitType = forecastUnitType;
        }
    }
}