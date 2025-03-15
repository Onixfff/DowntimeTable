using System;

namespace Downtime_table.Moduls.Data.Sources
{
    public class ArchivedDate : BaseDate
    {
        public ArchivedDate(int id, DateTime timestamp, TimeSpan difference, Recept recept, int? idTypeDowntime, string typeDownTime, string comments, bool isPastData)
            : base(id, timestamp, difference, recept, idTypeDowntime, typeDownTime, comments, isPastData) { }
    }
}
