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

        public bool _isPastData { get; set; }
        
        /// <summary>
        /// Возвращает новые данные
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timestamp"></param>
        /// <param name="difference"></param>
        public Date(int id, DateTime timestamp, TimeSpan difference)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            _isPastData = false;
            IdTypeDowntime = -1;
        }

        /// <summary>
        /// Возвращает данные занесенные в таблицу
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timestamp"></param>
        /// <param name="difference"></param>
        /// <param name="idTypeDowntime"></param>
        /// <param name="comments"></param>
        public Date(int id, DateTime timestamp, TimeSpan difference, int idTypeDowntime, string comments)
        {
            Id = id;
            Timestamp = timestamp;
            Difference = difference;
            IdTypeDowntime = idTypeDowntime;
            Comments = comments;
            _isPastData = true;
        }
    }
}
