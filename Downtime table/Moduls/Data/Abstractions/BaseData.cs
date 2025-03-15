using System;

namespace Downtime_table.Moduls.Data
{
    public abstract class BaseData
    {
        public int Id {get; private set;}
        public DateTime Timestamp {get; set;}
        public TimeSpan Difference {get; set;}
        public Recept Recept {get; set;}
        public int? IdTypeDowntime {get; set;}
        public string TypeDownTime {get; set;}
        public string Comments {get; set;}
        public bool IsPastData {get; set;}

        // Базовый конструктор
        protected BaseData(int id, DateTime timestamp, TimeSpan difference, Recept recept,
                          int? idTypeDowntime = null, string comment = null, string user = null)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            Recept = recept;
            IdTypeDowntime = idTypeDowntime;
            Comment = comment;
            User = user;
        }

        // Защищенный конструктор копирования
        public BaseData(BaseData other)
        {
            Id = other.Id;
            Timestamp = other.Timestamp;
            Difference = other.Difference;
            Recept = other.Recept;
            IdTypeDowntime = other.IdTypeDowntime;
            Comment = other.Comment;
            User = other.User;
        }
    }
}
