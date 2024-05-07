using MySql.Data.Types;
using System;

namespace Downtime_table
{
    public class Date
    {
        public int Id { get; private set; }

        public DateTime Timestamp { get; private set; }

        public TimeSpan Difference { get; private set; }

        public int IdTypeDowntime { get; private set; }

        public string TypeDowntime { get; private set; }

        public string Comments { get; private set; }

        public Date(int id, DateTime timestamp, TimeSpan difference)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
        }
    }
}
