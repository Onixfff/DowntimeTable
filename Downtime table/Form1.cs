using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class Form1 : Form
    {
        Database database = new Database();
        DateTime currentDate = DateTime.Now;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            DataSet ds = await database.GetMain(currentDate);
            dataGridView1.DataSource = ds.Tables[0];
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int targetId = 0;

            if(dataGridView1.CurrentCell.RowIndex >= 0)
            {
                int id;

                switch (dataGridView1.CurrentCell.ColumnIndex)
                {
                    case 2:
                        id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                        IdleSelection idleSelection = new IdleSelection(database, id);
                        idleSelection.Show();
                        break;
                    case 3:
                        id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                        string comment = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                        database.ChangeData(id, comment);
                        break;

                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            database.InsertData();
        }

    }

    public class Comments
    {
        List<string> _comments;

        public Comments(List<string> comments)
        {
            _comments = comments;
        }
    }
}
