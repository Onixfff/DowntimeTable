using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class Form1 : Form
    {
        private Database _database = new Database();
        private List<string> _comments;
        private DataSet _dataSet = new DataSet();
        private bool _isOpen = false;
        private int rowIndex;
        private int columnIndex;
        private bool isUpdate = false;
        private List<Recept> _recepts;
        private List<Date> mainDate;
        private DataSet _ds = new DataSet();
        private List<DateIdle> _idles = new List<DateIdle>();
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
            var color = Color.FromArgb(255,255,255);
            pictureBox1.BackColor = color;
            pictureBox1.BringToFront();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            logger.Trace("Form1_load > Start");
            DateTime _currentDate = DateTime.Now;
            await _database.GetMain(_currentDate, dataGridView1);

            _recepts = await _database.GetRecept();

            if (_recepts == null)
            {
                throw new Exception("Form1_Load (Ошибка _recepts == null)");
            }

            _idles = _database.GetIdles();
            
            if( _idles == null)
            {
                throw new Exception("Form1_Load (Ошибка idles == null)");
            }

            ChangeComboBoxRecepts();

            isUpdate = true;


            if (_ds == null)
            {
                MessageBox.Show("Ошибка получения данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                var time = _database.GetFullDowntime();
                labelTotal.Text = $"Итого : ({time.Days} : Дней)   ({time.Hours} : {time.Minutes} : {time.Seconds}) пропусков";
                button1.Enabled = true; 
                pictureBox1.SendToBack();
                isUpdate = false;
            }

            logger.Trace("Form1_load > End");
        }

        private void ChangeDataGridView(List<Date> date, string ReceptName)
        {
            string nameTable;
            _ds.Reset();
            nameTable = $"{ReceptName}";

            DataTable dt = new DataTable(nameTable);

            dt.Columns.Add(new DataColumn("id", typeof(int)));
            dt.Columns[0].ReadOnly = true;
            dt.Columns.Add(new DataColumn("Время начало", typeof(DateTime)));
            dt.Columns[1].ReadOnly = true;
            dt.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
            dt.Columns[2].ReadOnly = true;
            dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));
            dt.Columns.Add(new DataColumn("Рецепт", typeof(string)));
            dt.Columns[4].ReadOnly = true;



            if (ChecksDateWithinCurrentTime(nameTable))
            {
                for (int i = 0; i < date.Count; i++)
                {
                    if (date[i].Recept.Name == nameTable)
                    {
                        DataRow dr = dt.NewRow();
                        dr["id"] = date[i].Id;
                        dr["Время начало"] = date[i].Timestamp;
                        dr["Время простоя"] = date[i].Difference;
                        dr["Комментарий"] = date[i].Comments;
                        dr["Рецепт"] = date[i].Recept.Name;
                        dt.Rows.Add(dr);
                    }
                }
            }
            else
            {
                if (nameTable == "Все")
                {
                    for (int i = 0; i < date.Count; i++)
                    {
                        DataRow dr = dt.NewRow();
                        dr["id"] = date[i].Id;
                        dr["Время начало"] = date[i].Timestamp;
                        dr["Время простоя"] = date[i].Difference;
                        dr["Комментарий"] = date[i].Comments;
                        dr["Рецепт"] = date[i].Recept.Name;
                        dt.Rows.Add(dr);
                    }
                }
                else
                {
                    for (int i = 0; i < date.Count; i++)
                    {
                        if (date[i].Recept.Name == nameTable)
                        {
                            DataRow dr = dt.NewRow();
                            dr["id"] = date[i].Id;
                            dr["Время начало"] = date[i].Timestamp;
                            dr["Время простоя"] = date[i].Difference;
                            dr["Комментарий"] = date[i].Comments;
                            dr["Рецепт"] = date[i].Recept.Name;
                            dt.Rows.Add(dr);
                        }
                    }
                }

            }

            _ds.Tables.Add(dt);
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = _ds.Tables[nameTable];
            AddColumnTypeDownTime();
            ChangeIndexColumn();
            dataGridView1.Refresh();
        }

        private void ChangeIndexColumn()
        {
            dataGridView1.Columns["id"].DisplayIndex = 0;
            dataGridView1.Columns["Время начало"].DisplayIndex = 1;
            dataGridView1.Columns["Время простоя"].DisplayIndex = 2;
            dataGridView1.Columns["cmbVidProstoya"].DisplayIndex = 5;
            dataGridView1.Columns["Комментарий"].DisplayIndex = 4;
            dataGridView1.Columns["Рецепт"].DisplayIndex = 3;
        }

        private void AddColumnTypeDownTime()
        {
            DataGridViewComboBoxColumn cmbColumn = new DataGridViewComboBoxColumn();
            cmbColumn.HeaderText = "Вид простоя";
            cmbColumn.Name = "cmbVidProstoya";
            cmbColumn.DisplayMember = "TypeDowntime"; // Текст, который будет отображаться в ComboBox
            cmbColumn.ValueMember = "IdTypeDowntime"; // Значение, которое будет использоваться в качестве идентификатора

            foreach (var idle in _idles)
            {
                cmbColumn.Items.Add(new { IdTypeDowntime = idle.Id, TypeDowntime = idle.Name });
            }
            dataGridView1.Columns.Add(cmbColumn);

            List<Date> dates = _database.GetListDate();
            for (int i = 0; i < dates.Count; i++)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells["cmbVidProstoya"] is DataGridViewComboBoxCell comboBoxCell)
                    {
                        // Установка выбранного значения для ComboBox в каждой строке
                        if (dates[i].Timestamp == Convert.ToDateTime(row.Cells["Время начало"].Value))
                        {
                            var data = dates[i].IdTypeDowntime;
                            if (data != null && data > 0)
                            {
                                comboBoxCell.Value = data;
                            }
                            else
                            {
                                comboBoxCell = null;
                            }
                        }
                    }
                }
            }
        }

        private bool ChecksDateWithinCurrentTime(string receptName)
        {
            bool isHave = false;
            string nameTable = receptName;

            if (_ds.Tables.Contains(nameTable))
            {
                isHave = true;
            }

            return isHave;
        }

        private void ChangeComboBoxRecepts()
        {
            toolStripComboBox1.Items.Clear();

            toolStripComboBox1.Items.Add("Все");

            foreach (var item in _recepts)
            {
                toolStripComboBox1.Items.Add(item.Name);
            }

            toolStripComboBox1.SelectedIndex = 0;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Thread.Sleep(500);
            if (_database.ChecksFieldsAreFilledIn())
            {
                await _database.InsertDataAsync();
                Form1_Load(sender, e);
            }
            else
            {
                MessageBox.Show("Заполните все пустые поля", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button1.Enabled = true;
            }
        }

        private async void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.RowIndex >= 0)
            {
                if (dataGridView1.CurrentCell.ColumnIndex == 3)
                {
                    int currentRowIndex = dataGridView1.CurrentCell.RowIndex;
                    int currentColumnIndex = dataGridView1.CurrentCell.ColumnIndex;

                    TextBox tb = e.Control as TextBox;
                    if (tb != null)
                    {
                        var info = await _database.GetCommentsAsync();
                        
                        if(info != null)
                        {
                            if(_comments != null)
                            {
                                _comments.Clear();
                            }
                            _comments = new List<string>(info);
                        }

                        if(tb.Text.Length > 3)
                        {
                            if (_isOpen == false)
                            {
                                tableLayoutPanel2.ColumnStyles.Clear();
                                tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                                tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
                                _isOpen = true;
                            }
                        }
                        else
                        {
                            if (_isOpen == true)
                            {
                                tableLayoutPanel2.ColumnStyles.Clear();
                                tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                                tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
                                _isOpen = false;
                            }
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
                    case 5:
                        if (e.ColumnIndex == dataGridView1.Columns["cmbVidProstoya"].Index)
                        {
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            int idIdle = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["cmbVidProstoya"].Value);;
                            _database.ChangeData(id, idIdle);
                        }
                    break;
                    case 3:
                        if (e.ColumnIndex == dataGridView1.Columns["Комментарий"].Index)
                        {
                            columnIndex = e.ColumnIndex;
                            rowIndex = e.RowIndex;
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            var comment = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                            _database.ChangeData(id, comment);
                            if (comment.Length >= 3)
                            {
                                List<string> similar = FindMatchingSentences(comment);

                                if (_comments != null)
                                {
                                    if(similar.Count > 0)
                                    {
                                        if (_isOpen == false)
                                        {
                                            tableLayoutPanel2.ColumnStyles.Clear();
                                            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
                                            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
                                            _isOpen = true;
                                            ChangeDateGridView(similar);
                                        }
                                    }
                                    else
                                    {
                                        if(_isOpen == true)
                                        {
                                            tableLayoutPanel2.ColumnStyles.Clear();
                                            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                                            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
                                            _isOpen = false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if(_isOpen)
                                {
                                    tableLayoutPanel2.ColumnStyles.Clear();
                                    tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                                    tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
                                    _isOpen = false;
                                }
                            }
                        }
                    break;
                }
            }
        }

        private List<string> FindMatchingSentences(string input)
        {
            List<string> matchingSentences = new List<string>();
            string[] wor_ds = input.ToLower().Split(' ');
            foreach (string sentence in _comments)
            {
                bool matchFound = true;
                foreach (string word in wor_ds)
                {
                    if (!sentence.ToLower().Contains(word))
                    {
                        matchFound = false;
                        break;
                    }
                }
                if (matchFound)
                {
                    matchingSentences.Add(sentence);
                }
            }
            return matchingSentences;
        }

        private void ChangeDateGridView(List<string> comments)
        {
            _dataSet.Tables.Clear();
            DataTable dt = new DataTable();

            dt.Columns.Add(new DataColumn("Комментарии", typeof(string)));
            dt.Columns[0].ReadOnly = true;

            for (int i = 0; i < comments.Count; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Комментарии"] = comments[i];
                dt.Rows.Add(dr);
            }

            _dataSet.Clear();
            _dataSet.Tables.Add(dt);

            dataGridView2.DataSource = _dataSet.Tables[0];
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Попытка получить значение из ячейки
                try
                {
                    string comment = dataGridView2.Rows[e.RowIndex].Cells["Комментарии"].Value.ToString();
                    // Дополнительные действия с idIdle
                    dataGridView1.Rows[rowIndex].Cells[columnIndex].Value = comment;
                }
                catch (Exception ex)
                {
                    // Обработка ошибки преобразования или других исключений
                    MessageBox.Show("Ошибка при получении значения: " + ex.Message);
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (isUpdate == true)
                return;

            pictureBox1.BringToFront();
            pictureBox1.Visible = true;
            button1.Enabled = false;
            _database.ClearData();
            Thread.Sleep(500);
            Form1_Load(sender, e);
            button1.Enabled = true;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Даниил +79969592819", "Связь по ошибкам", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            CheckPasses.Form1 form1 = new CheckPasses.Form1();
            form1.ShowDialog();
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string textComboBox = toolStripComboBox1.SelectedItem.ToString();

            if (textComboBox == "Все")
            {
                ChangeDataGridView(_database.GetDate(), textComboBox);
                return;
            }

            for (int i = 0; i < _recepts.Count; i++)
            {

                if (_recepts[i].Name == textComboBox)
                {
                    ChangeDataGridView(_database.GetDate(), textComboBox);
                }
            }
        }
    }
}
