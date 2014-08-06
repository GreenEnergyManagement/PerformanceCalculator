using System;
using System.Collections;
using System.Collections.Generic;

namespace PerformanceCalculator
{
    public class UtcDateTimeEnumerator : IEnumerable<UtcDateTime>
    {
        protected UtcDateTime Start { get; set; }
        protected UtcDateTime End { get; set; }
        protected TimeResolution Resolution { get; set; }
        protected int TimeStepPeriod { get; set; }
        private UtcDateTimeEnumerator enumerator;

        protected UtcDateTimeEnumerator(UtcDateTime start, UtcDateTime end, TimeResolution resolution)
        {
            Start = start;
            End = end;
            Resolution = resolution;
            TimeStepPeriod = resolution.Type.ToMilliseconds() * resolution.IntervalStepInResolution;
        }

        public UtcDateTimeEnumerator(UtcDateTime start, UtcDateTime end, TimeResolutionType resolution, int interval = 1, ConditionalOperator comparer = ConditionalOperator.LtEq)
        {
            var res = TimeResolution.GetResolution(resolution, interval);
            if (comparer == ConditionalOperator.LtEq) enumerator = new UtcDateTimeEnumeratorLtEq(start, end, res);
            else if (comparer == ConditionalOperator.Lt) enumerator = new UtcDateTimeEnumeratorLt(start, end, res);
            else throw new NotImplementedException("Only implemented support for Lt (Less than) and LtEq (Less than or equal to) operators. If more operators are added, then additional implementations must be supplemented.");
        }

        public UtcDateTimeEnumerator(UtcDateTime start, UtcDateTime end, TimeResolution resolution, ConditionalOperator comparer = ConditionalOperator.LtEq)
        {
            if (comparer == ConditionalOperator.LtEq) enumerator = new UtcDateTimeEnumeratorLtEq(start, end, resolution);
            else if (comparer == ConditionalOperator.Lt) enumerator = new UtcDateTimeEnumeratorLt(start, end, resolution);
            else throw new NotImplementedException("Only implemented support for Lt (Less than) and LtEq (Less than or equal to) operators. If more operators are added, then additional implementations must be supplemented.");
        }

        public virtual IEnumerator<UtcDateTime> GetEnumerator()
        {
            if (enumerator == null) return new List<UtcDateTime>().GetEnumerator();
            return enumerator.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public enum ConditionalOperator
        {
            Lt,
            LtEq
        }

        private class UtcDateTimeEnumeratorLt : UtcDateTimeEnumerator
        {
            public UtcDateTimeEnumeratorLt(UtcDateTime start, UtcDateTime end, TimeResolution resolution) : base(start, end, resolution) { }

            public override IEnumerator<UtcDateTime> GetEnumerator()
            {
                for (UtcDateTime current = Start; current.UtcTime < End.UtcTime; current = new UtcDateTime(current.UtcTime.AddMilliseconds(TimeStepPeriod)))
                {
                    yield return current;
                }
            }
        }

        private class UtcDateTimeEnumeratorLtEq : UtcDateTimeEnumerator
        {
            public UtcDateTimeEnumeratorLtEq(UtcDateTime start, UtcDateTime end, TimeResolution resolution) : base(start, end, resolution) { }

            public override IEnumerator<UtcDateTime> GetEnumerator()
            {
                for (UtcDateTime current = Start; current.UtcTime <= End.UtcTime; current = new UtcDateTime(current.UtcTime.AddMilliseconds(TimeStepPeriod)))
                {
                    yield return current;
                }
            }
        }

        public static UtcDateTimeEnumerator EmptyEnumerator()
        {
            return new UtcDateTimeEnumerator(UtcDateTime.MinValue, UtcDateTime.MinValue, TimeResolution.GetResolution(TimeResolutionType.Milliseconds, 1));
        }
    }
}