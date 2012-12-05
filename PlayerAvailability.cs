using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace JOLTZ
{
    public class PlayerAvailability
    {
        public string Name { get; set; }
        public int GmtOffset { get; set; }
        public ObservableCollection<Availability> Availabilities { get; private set; }

        public PlayerAvailability()
        {
            Availabilities = new ObservableCollection<Availability>();
        }

        public PlayerAvailability(string availability)
            : this()
        {
            var items = availability.Split(',');
            items = items.Select(i => i.Trim()).ToArray();
            Name = items[0].Trim();
            if (!items[1].StartsWith("GMT"))
                throw new Exception("Invalid GMT");
            GmtOffset = Convert.ToInt32(items[1].Substring(3));
            for (var i = 2; i < items.Length; ++i)
            {
                bool isUncertain = items[i][0] == 'u';
                if (isUncertain)
                    items[i] = items[i].Substring(1);
                var ranges = items[i].Split('-', ':');
                AddAvailability(Convert.ToInt32(ranges[0]), Convert.ToInt32(ranges[1]), Convert.ToInt32(ranges[2]), Convert.ToInt32(ranges[3]), isUncertain);
            }
        }

        private void AddAvailability(int startHour, int startMinute, int endHour, int endMinute, bool isUncertain)
        {
            Availabilities.Add(new Availability(
                new DateTime(2012, 1, 1, GetUtcHour(startHour), startMinute, 0, 0, DateTimeKind.Utc),
                new DateTime(2012, 1, 1, GetUtcHour(endHour), endMinute, 0, 0, DateTimeKind.Utc),
                isUncertain));
        }

        public int GetUtcHour(int hour)
        {
            hour -= GmtOffset;
            if (hour < 0)
                hour += 24;
            else if (hour >= 24)
                hour -= 24;
            return hour;
        }

    }
}