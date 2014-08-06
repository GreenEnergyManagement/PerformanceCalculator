using System;

namespace PerformanceCalculator
{
    public class RestrictedRange<T> : IRestrictedRange<T> where T : IComparable
    {
        private T value;

        public T MinValue { get; private set; }
        public T MaxValue { get; private set; }

        public RestrictedRange(T minValue, T maxValue)
            : this(minValue, maxValue, minValue)
        {
        }

        public RestrictedRange(T minValue, T maxValue, T value)
        {
            if (minValue.CompareTo(maxValue) > 0)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }

            MinValue = minValue;
            MaxValue = maxValue;
            this.value = value;
        }

        public T Value
        {
            get { return value; }
            set
            {
                if ((0 < MinValue.CompareTo(value)) || (MaxValue.CompareTo(value) < 0))
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.value = value;
            }
        }

        public static implicit operator T(RestrictedRange<T> value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            return MinValue + " <= " + Value + " <= " + MaxValue;
        }
    }
}