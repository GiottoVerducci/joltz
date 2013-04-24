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
        private readonly int[] _presence = new int[60 * 24];
        public int[] Presence { get { return _presence; } }
        private readonly int[] _wePresence = new int[60 * 24];
        public int[] WePresence { get { return _wePresence; } }
        public bool IsDetailed { get; set; }

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

            int detailDelay;
            IsDetailed = Int32.TryParse(items[2], out detailDelay);
            if (IsDetailed)
            {
                var readIndex = 3;
                var writeIndex= 0;
                while (readIndex < items.Length && items[readIndex] != "we")
                {
                    var it = items[readIndex].Split('x');
                    var count = it.Length == 1 ? 1 : Convert.ToInt32(it[0]);
                    var value = Convert.ToInt32(it.Last());
                    while (count-- > 0)
                        for(int i = 0; i < detailDelay; ++i)
                            _presence[writeIndex++] = value;
                    ++readIndex;
                }
                ++readIndex;
                writeIndex = 0;
                while (readIndex < items.Length && items[readIndex] != "we")
                {
                    var it = items[readIndex].Split('x');
                    var count = it.Length == 1 ? 1 : Convert.ToInt32(it[0]);
                    var value = Convert.ToInt32(it.Last());
                    while (count-- > 0)
                        for (int i = 0; i < detailDelay; ++i)
                            _wePresence[writeIndex++] = value;
                    ++readIndex;
                }
            }
            else
            {
                for (var i = 2; i < items.Length; ++i)
                {
                    bool isUncertain = items[i][0] == 'u';
                    if (isUncertain)
                        items[i] = items[i].Substring(1);
                    bool isWeekEnd = items[i].StartsWith("we");
                    if (isWeekEnd)
                        items[i] = items[i].Substring(2);
                    var ranges = items[i].Split('-', ':');
                    AddAvailability(Convert.ToInt32(ranges[0]), Convert.ToInt32(ranges[1]), Convert.ToInt32(ranges[2]), Convert.ToInt32(ranges[3]), isUncertain, isWeekEnd);
                }
            }
        }

        private void AddAvailability(int startHour, int startMinute, int endHour, int endMinute, bool isUncertain, bool isWeekEnd)
        {
            Availabilities.Add(new Availability(
                new DateTime(2012, 1, 1, GetUtcHour(startHour), startMinute, 0, 0, DateTimeKind.Utc),
                new DateTime(2012, 1, 1, GetUtcHour(endHour), endMinute, 0, 0, DateTimeKind.Utc),
                isUncertain, isWeekEnd));
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