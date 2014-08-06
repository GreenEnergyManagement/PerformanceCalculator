using System;

namespace PerformanceCalculator
{
    /// <summary>
    /// Percent type, will add operators later as they are needed. Currently a consistent representation is only needed.
    /// </summary>
    public struct Percent
    {
        private readonly double value;

        public Percent(double value)
        {
            this.value = value;
        }

        public double ToDouble()
        {
            return value / 100;
        }

        public override string ToString()
        {
            string val = string.Empty;
            val = String.Format("{0,10:0.00}", value);
            return val + "%";
        }
    }
}