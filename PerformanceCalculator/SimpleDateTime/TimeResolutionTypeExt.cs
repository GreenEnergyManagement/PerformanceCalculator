using System;

namespace PerformanceCalculator
{
    public static class TimeResolutionTypeExt
    {
        public static int ToMilliseconds(this TimeResolutionType resolution)
        {
            if (resolution == TimeResolutionType.Milliseconds) return 1;
            if (resolution == TimeResolutionType.Seconds) return 1000;
            if (resolution == TimeResolutionType.Minute) return 60000;
            if (resolution == TimeResolutionType.Hour) return 3600000;
            if (resolution == TimeResolutionType.Day) return 86400000;
            throw new Exception("Unable to convert TimeResolution to milliseconds, the input resolution is: " + resolution);
        }

        public static TimeSpan ToTimeSpan(this TimeResolutionType resolution)
        {
            if (resolution == TimeResolutionType.Milliseconds) return new TimeSpan(0, 0, 0, 0, 1);
            if (resolution == TimeResolutionType.Seconds) return new TimeSpan(0, 0, 1);
            if (resolution == TimeResolutionType.Minute) return new TimeSpan(0, 1, 0);
            if (resolution == TimeResolutionType.Hour) return new TimeSpan(1, 0, 0);
            if (resolution == TimeResolutionType.Day) return new TimeSpan(1, 0, 0, 0);
            throw new Exception("Unable to convert TimeResolution to milliseconds, the input resolution is: " + resolution);
        }

        public static TimeSpan ToTimeSpan(this TimeResolutionType resolution, int steps)
        {
            if (resolution == TimeResolutionType.Milliseconds) return TimeSpan.FromMilliseconds(steps);
            if (resolution == TimeResolutionType.Seconds) return TimeSpan.FromSeconds(steps);
            if (resolution == TimeResolutionType.Minute) return TimeSpan.FromMinutes(steps);
            if (resolution == TimeResolutionType.Hour) return TimeSpan.FromHours(steps);
            if (resolution == TimeResolutionType.Day) return TimeSpan.FromDays(steps);
            throw new Exception("Unable to convert TimeResolution to milliseconds, the input resolution is: " + resolution);
        }
    }
}