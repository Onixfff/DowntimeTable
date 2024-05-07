using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Database database = new Database();
            DateTime currentDate = DateTime.Now;
            database.GetData(currentDate);
        }
    }
}
