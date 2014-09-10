namespace PerformanceCalculator
{
    public class ForecastMetaData
    {
        public readonly int ForecastTimeIndex;
        public readonly int ForecastValueIndex;
        public readonly int OffsetHoursAhead;
        public readonly char ForecastSep;
        public readonly string ForecastUnitType;
        public readonly string ForecastFileFilter;

        public ForecastMetaData(int forecastTimeIndex, int forecastValueIndex, int offsetHoursAhead, char forecastSep, string forecastUnitType, string forecastFileFilter)
        {
            ForecastTimeIndex = forecastTimeIndex;
            ForecastValueIndex = forecastValueIndex;
            OffsetHoursAhead = offsetHoursAhead;
            ForecastSep = forecastSep;
            ForecastUnitType = forecastUnitType;
            ForecastFileFilter = forecastFileFilter;
        }
    }
}