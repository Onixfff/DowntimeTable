using System;

namespace Downtime_table.Moduls.Data
{
    public abstract class BaseDate
    {
        public int Id {get; set;}
        public DateTime Timestamp {get; set;}
        public TimeSpan Difference {get; set;}
        public Recept Recept {get; set;}
        public int? IdTypeDowntime {get; set;}
        public string TypeDownTime {get; set;}
        public string Comment { get; set;}
        public bool IsPastData { get; set; }

        // Базовый конструктор
        protected BaseDate(int id, DateTime timestamp, TimeSpan difference, Recept recept,
                          int? idTypeDowntime = null, string comment = null, string comments = null, bool isPastData = false)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            Recept = recept;
            IdTypeDowntime = idTypeDowntime;
            Comment = comment;
            IsPastData = isPastData;
        }

        // Защищенный конструктор копирования
        public BaseDate(BaseDate other)
        {
            Id = other.Id;
            Timestamp = other.Timestamp;
            Difference = other.Difference;
            Recept = other.Recept;
            IdTypeDowntime = other.IdTypeDowntime;
            Comment = other.Comment;
            IsPastData = other.IsPastData;
        }
    }
}
