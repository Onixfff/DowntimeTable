﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class Form1 : Form
    {
        private Database _database = new Database();
        private DateTime _currentDate = DateTime.Now;
        private List<string> _comments;
        private ViewComments _viewComments;

        public Form1()
        {
            InitializeComponent();
            _viewComments = new ViewComments();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            DataSet ds = await _database.GetMain(_currentDate, dataGridView1);
            dataGridView1.DataSource = ds.Tables[0];

            List<DateIdle> idles = await _database.GetIdles();

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

            if (!_database.GetBoolIsNewData())
            {
                List<Date> dates = _database.GetListDate();
                for(int i = 0; i < dates.Count; i++)
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.Cells["cmbVidProstoya"] is DataGridViewComboBoxCell comboBoxCell)
                        {
                            // Установка выбранного значения для ComboBox в каждой строке
                            if (dates[i].Id == Convert.ToInt32(row.Cells["id"].Value))
                                comboBoxCell.Value = dates[i].IdTypeDowntime; // Установить значение на "Значение 1"
                        }
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _database.InsertData();
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
                        var info = await _database.GetComments();
                        
                        if(info != null)
                        {
                            if(_comments != null)
                            {
                                _comments.Clear();
                            }
                            _comments = new List<string>(info);
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
                            _database.ChangeData(id, idIdle);
                        }
                    break;
                    case 3:
                        if (e.ColumnIndex == dataGridView1.Columns["Комментарий"].Index)
                        {
                            id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells[targetId].Value);
                            var comment = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                            _database.ChangeData(id, comment);

                            if (comment.Length > 3)
                            {
                                bool isUpdate = false;
                                if (_viewComments.IsDisposed == true || _viewComments.Visible == false)
                                {
                                    _viewComments = new ViewComments();
                                    isUpdate = true;
                                }

                                List<string> similar = FindMatchingSentences(comment);
                                if (_comments != null && isUpdate == false)
                                {
                                    _viewComments.ChangeListComments(similar);
                                }
                                else
                                {
                                    _viewComments.ChangeListComments(similar);
                                    _viewComments.Show();
                                }

                                dataGridView1.Focus(); // Устанавливаем фокус обратно на DataGridView
                            }
                        }
                    break;
                }
            }
        }

        private List<string> FindMatchingSentences(string input)
        {
            List<string> matchingSentences = new List<string>();
            string[] words = input.ToLower().Split(' ');
            foreach (string sentence in _comments)
            {
                bool matchFound = true;
                foreach (string word in words)
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
    }
}
