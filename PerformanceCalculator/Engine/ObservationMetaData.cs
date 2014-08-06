namespace PerformanceCalculator
{
    public class ObservationMetaData
    {
        public readonly int ObservationTimeIndex;
        public readonly int ObservationValueIndex;
        public readonly char ObservationSep;
        public readonly string ObservationUnitType;
        public readonly double NormalizationValue;

        public ObservationMetaData(int observationTimeIndex, int observationValueIndex, char observationSep, string observationUnitType, double normalizationValue)
        {
            ObservationTimeIndex = observationTimeIndex;
            ObservationValueIndex = observationValueIndex;
            ObservationSep = observationSep;
            ObservationUnitType = observationUnitType;
            NormalizationValue = normalizationValue;
        }    
    }
}