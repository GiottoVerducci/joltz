using System;

namespace JOLTZ
{
    public class Availability
    {
        public DateTime UtcStartTime { get; private set; }
        public DateTime UtcEndTime { get; private set; }
        public bool IsUncertain { get; private set; }
        public bool IsWeekEnd { get; private set; }

        public Availability(DateTime utcStartTime, DateTime utcEndTime, bool isUncertain, bool isWeekEnd)
        {
            UtcStartTime = utcStartTime;
            UtcEndTime = utcEndTime;
            IsUncertain = isUncertain;
            IsWeekEnd = isWeekEnd;
        }

        public bool Contains(double currentTime)
        {
            var start = UtcStartTime.ToLocalTime().TimeOfDay.TotalMinutes;
            var end = UtcEndTime.ToLocalTime().TimeOfDay.TotalMinutes;
            if (end < start)
                return start <= currentTime || currentTime <= end;
            return start <= currentTime && currentTime <= end;
        }

        public bool Overlaps(Availability other)
        {
            return IsBetween(UtcStartTime.TimeOfDay, other.UtcStartTime.TimeOfDay, other.UtcEndTime.TimeOfDay)
                || IsBetween(UtcEndTime.TimeOfDay, other.UtcStartTime.TimeOfDay, other.UtcEndTime.TimeOfDay)
                || IsBetween(other.UtcStartTime.TimeOfDay, UtcStartTime.TimeOfDay, UtcEndTime.TimeOfDay)
                || IsBetween(other.UtcEndTime.TimeOfDay, UtcStartTime.TimeOfDay, UtcEndTime.TimeOfDay);
        }

        private bool IsBetween(TimeSpan time, TimeSpan start, TimeSpan end)
        {
            if (start <= end)
                return time >= start && time <= end;
            return time >= start || time <= end; //eg. 23:00 - 6:00
        }

    }
}