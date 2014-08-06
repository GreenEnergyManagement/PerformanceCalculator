namespace PerformanceCalculator
{
    public class RestrictedRangeDouble : IRestrictedRange<double>
    {
        private readonly RestrictedRange<double> range;

        public RestrictedRangeDouble(double minValue, double maxValue, double value)
            : this(new RestrictedRange<double>(minValue, maxValue, value))
        {
        }

        public RestrictedRangeDouble(RestrictedRange<double> range)
        {
            this.range = range;
        }

        public double Value
        {
            get { return range.Value; }
            set { range.Value = value; }
        }

        public Percent ToPercent()
        {
            double diff = range.MaxValue - range.MinValue;
            double obsDiff = range.Value - range.MinValue;
            double percent = obsDiff / diff * 100;
            return new Percent(percent);
        }
    }
}