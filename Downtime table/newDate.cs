using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downtime_table
{
    public class newDate
    {
        public int DBIG;
        public DateTime DateTime;
        public string NameRecept;

        public newDate(int dbig, DateTime dateTime, string nameRecept)
        {
            DBIG = dbig;
            DateTime = dateTime;
            NameRecept = nameRecept;
        }
    }
}
