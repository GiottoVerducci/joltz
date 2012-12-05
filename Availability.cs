using System;

namespace JOLTZ
{
    public class Availability
    {
        public DateTime UtcStartTime { get; private set; }
        public DateTime UtcEndTime { get; private set; }

        public Availability(DateTime utcStartTime, DateTime utcEndTime)
        {
            UtcStartTime = utcStartTime;
            UtcEndTime = utcEndTime;
        }

        public bool Contains(double currentTime)
        {
            var start = UtcStartTime.ToLocalTime().TimeOfDay.TotalMinutes;
            var end = UtcEndTime.ToLocalTime().TimeOfDay.TotalMinutes;
            if (end < start)
                return start <= currentTime || currentTime <= end;
            return start <= currentTime && currentTime <= end;
        }
    }
}