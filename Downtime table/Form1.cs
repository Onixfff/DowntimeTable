using System;
using System.Data;
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

        private async void button1_Click(object sender, EventArgs e)
        {
            Database database = new Database();
            DateTime currentDate = DateTime.Now;
            DataSet ds = await database.GetData(currentDate);
            dataGridView1.DataSource = ds.Tables[0];
        }
    }
}
