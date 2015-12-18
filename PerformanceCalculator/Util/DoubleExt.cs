using System;
using System.Globalization;

namespace PerformanceCalculator
{
    public static class DoubleExt
    {
        /// <summary>
        /// Checks equality up to specified digits precision. 
        ///      
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="digitsPrecision"></param>
        /// <returns></returns>
        public static bool AboutEqual(this double x, double y, int digitsPrecision = 10)
        {
            return Math.Round(x, digitsPrecision) == Math.Round(y, digitsPrecision);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/2411392/double-epsilon-for-equality-greater-than-less-than-less-than-or-equal-to-gre
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool AboutEqualEpsilon(this double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-10;
            return Math.Abs(x - y) <= epsilon;
        }

        public static double Round(this double d, int digits)
        {
            return Math.Round(d, digits);
        }

        public static string ToPrintableFormat(this double value, DecimalSeperator decimalSeperator = DecimalSeperator.Dot)
        {
            string val = string.Empty;
            if (!double.IsInfinity(value) /*&& (value > 0.0 || value < 0.0)*/) val = value.ToString("0.000", CultureInfo.InvariantCulture);
            if (decimalSeperator == DecimalSeperator.Comma) val = val.Replace(".", ",");

            return val;
        }

        public static string ToPrintableFormat(this double? value, DecimalSeperator decimalSeperator = DecimalSeperator.Dot)
        {
            string val = string.Empty;
            if (value.HasValue && !double.IsInfinity(value.Value)) val = value.Value.ToString("0.000", CultureInfo.InvariantCulture);
            if (decimalSeperator == DecimalSeperator.Comma) val = val.Replace(".", ",");

            return val;
        }

        public static string ToPrintableFormat(this double value, string doubleFormat)
        {
            string val = string.Empty;
            if (!double.IsInfinity(value) /*&& (value > 0.0 || value < 0.0)*/) val = value.ToString(doubleFormat, CultureInfo.InvariantCulture);
            return val;
        }

        public static string ToPrintableFormat(this double? value, string doubleFormat)
        {
            string val = string.Empty;
            if (value.HasValue && !double.IsInfinity(value.Value)) val = value.Value.ToString(doubleFormat, CultureInfo.InvariantCulture);
            return val;
        }
    }

    public enum DecimalSeperator
    {
        Comma,
        Dot
    }
}