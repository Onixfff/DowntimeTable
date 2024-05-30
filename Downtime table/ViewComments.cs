using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Downtime_table
{
    public partial class ViewComments : Form
    {
        private List<string> _comments;
        private DataSet _dataSet = new DataSet();

        public ViewComments()
        {
            InitializeComponent();
            this.TopMost = true;
        }

        private void ViewComments_Load(object sender, EventArgs e)
        {
            ChangeDateGridView();
        }

        public void ChangeListComments(List<string> comments)
        {
            if (comments != null)
            {
                _comments = comments;
            }

            ChangeDateGridView();
        }

        private void ChangeDateGridView()
        {
            _dataSet.Tables.Clear();
            DataTable dt = new DataTable();

            dt.Columns.Add(new DataColumn("Комментирии", typeof(string)));
            dt.Columns[0].ReadOnly = true;

            for (int i = 0; i < _comments.Count; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Комментирии"] = _comments[i];
                dt.Rows.Add(dr);
            }

            _dataSet.Clear();
            _dataSet.Tables.Add(dt);

            dataGridView1.DataSource = _dataSet.Tables[0];
        }
    }

    
}
