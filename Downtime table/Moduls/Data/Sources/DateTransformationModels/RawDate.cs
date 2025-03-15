using System;

namespace Downtime_table
{
    public class RawDate
    {
        public int DBIG;
        public DateTime DateTime;
        public string NameRecept;

        public RawDate(int dbig, DateTime dateTime, string nameRecept)
        {
            DBIG = dbig;
            DateTime = dateTime;
            NameRecept = nameRecept;
        }
    }
}
