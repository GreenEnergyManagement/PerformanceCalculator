using System;

namespace PerformanceCalculator
{
    public static class TimeSpanExt
    {
        public static TimeResolutionType ToTimeResolution(this TimeSpan span, out int steps)
        {
            if (span.TotalDays >= 1)
            {
                steps = span.Days;
                return TimeResolutionType.Day;
            }
            if (span.TotalHours >= 1)
            {
                steps = span.Hours;
                return TimeResolutionType.Hour;
            }
            if (span.TotalMinutes >= 1)
            {
                steps = span.Minutes;
                return TimeResolutionType.Minute;
            }
            if (span.TotalMilliseconds >= 1)
            {
                steps = span.Milliseconds;
                return TimeResolutionType.Milliseconds;
            }

            throw new Exception("Unable to resolve TimeSpan to a TimeResolution. TimeSpan: " + span.TotalMilliseconds);
        }
    }
}