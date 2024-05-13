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
            DataSet ds = await database.GetMain(currentDate, dataGridView1);
            dataGridView1.DataSource = ds.Tables[0];

            List<DateIdle> idles = await database.GetIdles();

            DataGridViewComboBoxColumn cmbColumn = new DataGridViewComboBoxColumn();
            cmbColumn.HeaderText = "Вид простоя";
            cmbColumn.Name = "cmbVidProstoya";
            cmbColumn.DisplayMember = "TypeDowntime"; // Текст, который будет отображаться в ComboBox
            cmbColumn.ValueMember = "IdTypeDowntime"; // Значение, которое будет использоваться в качестве идентификатора

            foreach (var idle in idles)
            {
                cmbColumn.Items.Add(new { IdTypeDowntime = idle.Id, TypeDowntime = idle.Name });
            }

            dataGridView1.Columns.Add(cmbColumn);

            dataGridView1.Columns["id"].DisplayIndex = 0;
            dataGridView1.Columns["Время начала"].DisplayIndex = 1;
            dataGridView1.Columns["Время простоя"].DisplayIndex = 2;
            dataGridView1.Columns["cmbVidProstoya"].DisplayIndex = 3;
            dataGridView1.Columns["Комментарий"].DisplayIndex = 4;


        }

        private void button1_Click(object sender, EventArgs e)
        {
            database.InsertData();
        }

        private async void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.RowIndex >= 0)
            {
                if (dataGridView1.CurrentCell.ColumnIndex == 3)
                {
                    TextBox tb = e.Control as TextBox;
                    if (tb != null)
                    {
                        tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                        AutoCompleteStringCollection data = new AutoCompleteStringCollection();
                        var info = await database.GetComments();
                        
                        if(info != null)
                        {
                            data.AddRange(info);
                            tb.AutoCompleteCustomSource = data;
                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int targetId = 0;

            if (dataGridView1.CurrentCell.RowIndex >= 0)
            {
                int id;

                switch (dataGridView1.CurrentCell.ColumnIndex)
                {
                    case 4:
                        if (e.ColumnIndex == dataGridView1.Columns["cmbVidProstoya"].Index)
                        {
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            int idIdle = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["cmbVidProstoya"].Value);;
                            Console.WriteLine("Выбранное ID: " + id.ToString() + "Выбранный IdIdle: " + idIdle.ToString());
                            database.ChangeData(id, idIdle);
                        }
                    break;
                    case 3:
                        if (e.ColumnIndex == dataGridView1.Columns["Комментарий"].Index)
                        {
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            var comment = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                            database.ChangeData(id, comment);
                        }
                    break;
                }
            }
        }
    }
}
