using System;

namespace PerformanceCalculator
{
    public interface IRestrictedRange<T> where T : IComparable
    {
        T Value { get; set; }
    }
}