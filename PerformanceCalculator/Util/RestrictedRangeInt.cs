namespace PerformanceCalculator
{
    public class RestrictedRangeInt : IRestrictedRange<int>
    {
        private readonly RestrictedRange<int> range;

        public RestrictedRangeInt(int minValue, int maxValue, int value)
            : this(new RestrictedRange<int>(minValue, maxValue, value))
        {
        }

        public RestrictedRangeInt(RestrictedRange<int> range)
        {
            this.range = range;
        }

        public int Value
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