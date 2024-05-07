using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downtime_table
{
    public class DateIdle
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public DateIdle(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
