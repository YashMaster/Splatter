using System;

namespace Splatter
{
    public class Range<T> where T : IComparable<T>
    {
        private Random Rando = new Random();
        public T Min { get; set; }
        public T Max { get; set; }

        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            return "(" + Min + ", " + Max + ")";
        }

        public Boolean IsValid()
        {
            return Min.CompareTo(Max) <= 0;
        }

        public Boolean ContainsValue(T value)
        {
            return (Min.CompareTo(value) <= 0) && (value.CompareTo(Max) <= 0);
        }
        
    }

}
