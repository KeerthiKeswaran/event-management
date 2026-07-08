using System;

namespace Event.Models
{
    public struct DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public bool Overlaps(DateRange other)
        {
            return Start < other.End && other.Start < End;
        }

        public bool Contains(DateTime dateTime)
        {
            return dateTime >= Start && dateTime <= End;
        }
    }
}
