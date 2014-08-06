using System;

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
    }
}