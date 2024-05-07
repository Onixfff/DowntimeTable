using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class IdleSelection : Form
    {
        private int _id;
        private Database _database;

        public IdleSelection(Database database, int id)
        {
            _id = id;
            _database = database;
            InitializeComponent();
        }

        private async void IdleSelection_Load(object sender, EventArgs e)
        {
            try
            {
                DataSet dataSet = await _database.GetIdles();
                if (dataSet != null)
                    dataGridView1.DataSource = dataSet.Tables[0];
                else
                    MessageBox.Show("Ошибка получения данных");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.Close();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.CurrentCell.RowIndex >= 0)
            {
                int targetId = 0;

                if (dataGridView1.CurrentCell.RowIndex >= 0)
                {
                    int id;

                    switch (dataGridView1.CurrentCell.ColumnIndex)
                    {
                        case 2:
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            break;
                        case 3:
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            break;

                    }
                }
            }
        }
    }
}
