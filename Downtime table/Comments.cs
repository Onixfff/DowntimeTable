using System.Collections.Generic;

namespace Downtime_table
{
    public partial class Form1
    {
        public class Comments
        {
            List<string> _comments;

            public Comments(List<string> comments)
            {
                _comments = comments;
            }
        }
    }
}
