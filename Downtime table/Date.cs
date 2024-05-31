using MySql.Data.Types;
using Mysqlx.Crud;
using System;

namespace Downtime_table
{
    public class Date
    {
        public int Id { get; private set; }

        public DateTime Timestamp { get; private set; }

        public TimeSpan Difference { get; private set; }

        public int IdTypeDowntime { get; set; }

        public string Comments { get; set; }

        public bool IsUpdate { get; set; }

        public Date(int id, DateTime timestamp, TimeSpan difference, bool isUpdate)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            
        }

        public Date(int id, DateTime timestamp, TimeSpan difference, int idTypeDowntime, string comments, bool isUpdate)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            IdTypeDowntime = idTypeDowntime;
            Comments = comments;
            IsUpdate = isUpdate;
        }
    }
}
