using System;

namespace Downtime_table.Moduls.Data.Sources
{
    public class ProcessedDate :BaseDate
    {
        public ProcessedDate(int id, DateTime timestamp, TimeSpan difference, Recept recept): base(id, timestamp, difference, recept, -1, null, null, false) { }
    }
}
