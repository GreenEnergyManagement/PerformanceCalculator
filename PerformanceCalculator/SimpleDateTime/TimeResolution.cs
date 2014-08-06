using System;

namespace PerformanceCalculator
{
    public struct TimeResolution
    {
        public static readonly TimeResolution Millisecond;
        public static readonly TimeResolution Second;
        public static readonly TimeResolution Minute;
        public static readonly TimeResolution Minute5th;
        public static readonly TimeResolution Minute6th;
        public static readonly TimeResolution Minute10th;
        public static readonly TimeResolution Minute12th;
        public static readonly TimeResolution Minute15th;
        public static readonly TimeResolution Minute20th;
        public static readonly TimeResolution Minute30th;
        public static readonly TimeResolution Hour;
        public static readonly TimeResolution Day;
        public static readonly TimeResolution Week;
        public static readonly TimeResolution Month;
        public static readonly TimeResolution Quarter;
        public static readonly TimeResolution Season;
        public static readonly TimeResolution Year;

        public readonly TimeResolutionType Type;
        public readonly int IntervalStepInResolution;

        public TimeSpan ToTimeSpan()
        {
            TimeSpan resolution = Type.ToTimeSpan(IntervalStepInResolution);
            return resolution;
        }

        public static TimeResolution FromMilliseconds(long milliseconds)
        {
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            int steps;
            TimeResolutionType timeResolutionType = timeSpan.ToTimeResolution(out steps);
            TimeResolution resolution = GetResolution(timeResolutionType, steps);
            return resolution;
        }

        public static TimeResolution CustomResolution(TimeResolutionType type, int stepsInPeriod)
        {
            return new TimeResolution(type, stepsInPeriod);
        }

        internal TimeResolution(TimeResolutionType type, int stepsInPeriod)
        {
            Type = type;
            IntervalStepInResolution = stepsInPeriod;
        }

        /// <summary>
        /// Static constructor initializing the defined databased to hardcoded values.
        /// </summary>
        static TimeResolution()
        {
            Millisecond = new TimeResolution(TimeResolutionType.Milliseconds, 1);
            Second = new TimeResolution(TimeResolutionType.Seconds, 1);
            Minute = new TimeResolution(TimeResolutionType.Minute, 1);
            Minute5th = new TimeResolution(TimeResolutionType.Minute, 5);
            Minute6th = new TimeResolution(TimeResolutionType.Minute, 6);
            Minute10th = new TimeResolution(TimeResolutionType.Minute, 10);
            Minute12th = new TimeResolution(TimeResolutionType.Minute, 12);
            Minute15th = new TimeResolution(TimeResolutionType.Minute, 15);
            Minute20th = new TimeResolution(TimeResolutionType.Minute, 20);
            Minute30th = new TimeResolution(TimeResolutionType.Minute, 30);
            Hour = new TimeResolution(TimeResolutionType.Hour, 1);
            Day = new TimeResolution(TimeResolutionType.Day, 1);
            Week = new TimeResolution(TimeResolutionType.Day, 7);
            Month = new TimeResolution(TimeResolutionType.Day, 30); // Inaccurate
            Quarter = new TimeResolution(TimeResolutionType.Day, 91); // More Inaccurate
            Season = new TimeResolution(TimeResolutionType.Day, 182); // More Inaccurate
            Year = new TimeResolution(TimeResolutionType.Day, 365); // Inaccurate
        }

        public static TimeResolution GetResolution(TimeResolutionType resolution, int stepsInPeriod)
        {
            if (Millisecond.Type == resolution && Millisecond.IntervalStepInResolution == stepsInPeriod) return Millisecond;
            if (Second.Type == resolution && Second.IntervalStepInResolution == stepsInPeriod) return Second;
            if (Minute.Type == resolution && Minute.IntervalStepInResolution == stepsInPeriod) return Minute;
            if (Minute5th.Type == resolution && Minute5th.IntervalStepInResolution == stepsInPeriod) return Minute5th;
            if (Minute6th.Type == resolution && Minute6th.IntervalStepInResolution == stepsInPeriod) return Minute6th;
            if (Minute10th.Type == resolution && Minute10th.IntervalStepInResolution == stepsInPeriod) return Minute10th;
            if (Minute12th.Type == resolution && Minute12th.IntervalStepInResolution == stepsInPeriod) return Minute12th;
            if (Minute15th.Type == resolution && Minute15th.IntervalStepInResolution == stepsInPeriod) return Minute15th;
            if (Minute20th.Type == resolution && Minute20th.IntervalStepInResolution == stepsInPeriod) return Minute20th;
            if (Minute30th.Type == resolution && Minute30th.IntervalStepInResolution == stepsInPeriod) return Minute30th;
            if (Hour.Type == resolution && Hour.IntervalStepInResolution == stepsInPeriod) return Hour;
            if (Day.Type == resolution && Day.IntervalStepInResolution == stepsInPeriod) return Day;
            if (Week.Type == resolution && Week.IntervalStepInResolution == stepsInPeriod) return Week;
            if (Month.Type == resolution && Month.IntervalStepInResolution == stepsInPeriod) return Month;
            if (Quarter.Type == resolution && Quarter.IntervalStepInResolution == stepsInPeriod) return Quarter;
            if (Season.Type == resolution && Season.IntervalStepInResolution == stepsInPeriod) return Season;
            if (Year.Type == resolution && Year.IntervalStepInResolution == stepsInPeriod) return Year;

            return CustomResolution(resolution, stepsInPeriod);
        }

        public bool IsValid(UtcDateTime time)
        {
            return IsValid(time, this, ToTimeSpan());
        }

        public static bool IsValid(UtcDateTime time, TimeResolution res, TimeSpan resolutionSpan)
        {
            var span = time.UtcTime.TimeOfDay;
            var span2 = resolutionSpan;
            if (Millisecond.ToTimeSpan() == resolutionSpan) return true;
            if (Second.ToTimeSpan() == resolutionSpan) return EqualMilliseconds(span, span2);
            if (Minute.ToTimeSpan() == resolutionSpan) return EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute5th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute5th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute6th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute6th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute10th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute10th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute12th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute12th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute15th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute15th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute20th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute20th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Minute30th.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, Minute30th.IntervalStepInResolution) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            if (Hour.ToTimeSpan() == resolutionSpan) return EqualMinutes(span, span2, 0) && EqualSeconds(span, span2) && EqualMilliseconds(span, span2);
            // Cannot implement the rest without time zone data and probably a base time point.

            throw new Exception("Unable to verify TimeResolution.Type and cannot infer provided time's resolution. Type: " + res.Type);
        }

        private static bool EqualMilliseconds(TimeSpan span, TimeSpan resolutionSpan)
        {
            return span.Milliseconds == resolutionSpan.Milliseconds;
        }

        private static bool EqualSeconds(TimeSpan span, TimeSpan resolutionSpan)
        {
            return span.Seconds == resolutionSpan.Seconds;
        }

        private static bool EqualMinutes(TimeSpan span, TimeSpan span2, int stepsInPeriod)
        {
            int min1 = span.Minutes;
            int min2 = span2.Minutes;

            if (stepsInPeriod == 0) return min1 == 0;
            if (min1 == min2) return true;

            bool val = TimeSpan.FromMinutes(min1).Ticks % TimeSpan.FromMinutes(min2).Ticks == 0;
            return val;
        }
    }
}