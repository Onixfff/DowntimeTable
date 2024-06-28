using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Windows.Forms;
using System.Data;
using System.Globalization;
using LiveCharts;
using LiveCharts.Wpf;

namespace CheckPasses
{
    public partial class Form1 : Form
    {
        private string _conn;
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["conn5"].ConnectionString);
        private DataSet _ds = new DataSet();
        private DateTime _thisDate = DateTime.Now;
        private DowntimeAnalyzer _analyzer = new DowntimeAnalyzer();

        private enum MountEnum
        {
            Январь,
            Февраль,
            Март,
            Апрель,
            Май,
            Июнь,
            Июль,
            Август,
            Сентабрь,
            Октябрь,
            Ноябрь,
            Декабрь
        };

        public Form1()
        {
            InitializeComponent();
            pieChart1.Visible = false;
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            numericUpDown1.Minimum = 2000;
            numericUpDown1.Maximum = dateTimePicker_before.Value.Year;
            numericUpDown1.Value = dateTimePicker_before.Value.Year;
            ChangeStartDate();
            ChangeViewDataGridView();
            ChangeComboBoxItems();
        }

        private void ChangeComboBoxItems()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.Add("Стандартный");
            comboBox1.Items.Add("Диаграмма");
            //comboBox1.Items.Add("Причины");
            comboBox1.SelectedIndex = 0;
        }

        private void ChangeStartDate()
        {
            DateTime now = DateTime.Now;

            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            dateTimePicker_from.Value = startDate;
            dateTimePicker_before.Value = endDate;

        }

        private async void ChangeViewDataGridView()
        {
            DateTime startDate, endDate;
            double year;

            year = ReturnStartEndDate(DateTime.Now.Month, out startDate, out endDate);
            string nameTable = $"Начало:{startDate}\nКонец:{endDate}\nГод:{year}";

            try
            {
                try
                {
                    await _mCon.OpenAsync();
                }
                catch
                {
                    goto Select;
                }

            Select:

                DataTable dt = new DataTable(nameTable);

                dt.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                dt.Columns[0].ReadOnly = true;
                dt.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                dt.Columns[1].ReadOnly = true;
                dt.Columns.Add(new DataColumn("Вид простоя", typeof(string)));
                dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                string query = "SELECT Timestamp, Difference, name, Comment FROM spslogger.downtime\r\nleft join ididles on\r\ndowntime.idIdle = ididles.id where downtime.Timestamp >= @startDate and downtime.Timestamp <= @endDate";

                using (MySqlCommand cmd = new MySqlCommand(query, _mCon)) 
                {
                    cmd.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            DataRow dr = dt.NewRow();
                            dr["Время начала"] = reader.GetDateTime(0);
                            dr["Время простоя"] = reader.GetTimeSpan(1);
                            dr["Вид простоя"] = reader.GetString(2);
                            dr["Комментарий"] = reader.GetString(3);
                            dt.Rows.Add(dr);
                        }
                        reader.Close();

                        _analyzer.ClearData();
                        _analyzer.AnalyzeDowntime(dt);
                        ChangeDiagram();
                        var time = _analyzer.GetTotalTime();
                        labelTotal.Text = $"Итого : ({time.Days} : Дней)   ({time.Hours}:{time.Minutes}:{time.Seconds}) пропусков";

                        _ds.Tables.Add(dt);
                    } 
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }

            dataGridView1.DataSource = _ds.Tables[nameTable];
        }

        private async void ChangeViewDataGridView(DateTime startDate, DateTime endDate, int year)
        {
            bool isThisTime, isHave;
            string nameTable = $"Начало:{startDate}\nКонец:{endDate}\nГод:{year}";
            isThisTime = ChecksDateWithinCurrentTime(startDate, endDate, year, out isHave);
            
            if(isThisTime == true || isHave == true)
            {
                dataGridView1.DataSource = _ds.Tables[nameTable];

                _analyzer.ClearData();
                _analyzer.AnalyzeDowntime(_ds.Tables[nameTable]);
                ChangeDiagram();
            }
            else
            {
                try
                {
                    try
                    {
                        if(_mCon.State == ConnectionState.Closed)
                            await _mCon.OpenAsync();
                    }
                    catch
                    {
                        goto Get;
                    }

                Get:

                    DataTable dt = new DataTable(nameTable);

                    dt.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                    dt.Columns[0].ReadOnly = true;
                    dt.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                    dt.Columns[1].ReadOnly = true;
                    dt.Columns.Add(new DataColumn("Вид простоя", typeof(string)));
                    dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                    string query = "SELECT Timestamp, Difference, name, Comment FROM spslogger.downtime\r\nleft join ididles on\r\ndowntime.idIdle = ididles.id \r\nwhere downtime.Timestamp >= @startDate and downtime.Timestamp <= @endDate";

                    using (MySqlCommand cmd = new MySqlCommand(query, _mCon))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                DataRow dr = dt.NewRow();
                                dr["Время начала"] = reader.GetDateTime(0);
                                dr["Время простоя"] = reader.GetTimeSpan(1);
                                dr["Вид простоя"] = reader.GetString(2);
                                dr["Комментарий"] = reader.GetString(3);
                                dt.Rows.Add(dr);
                            }
                            reader.Close();

                            _analyzer.ClearData();
                            _analyzer.AnalyzeDowntime(dt);
                            ChangeDiagram();


                            _ds.Tables.Add(dt);
                            labelTotal.Text = $"Итоги : {_analyzer.GetTotalTime()} пропусков";
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally { await _mCon.CloseAsync(); }

                dataGridView1.DataSource = _ds.Tables[nameTable];
            }
        }

        private bool ChecksDateWithinCurrentTime(DateTime startDate, DateTime endDate, int year, out bool isHave)
        {
            bool isThisTime = false;
            isHave = false;
            string nameTable = $"Начало:{startDate}\nКонец:{endDate}\nГод:{year}";
            if (_ds.Tables.Contains(nameTable))
            {
                isHave = true;

                if (_thisDate >= startDate && _thisDate <= endDate)
                {
                    isThisTime = true;
                }
            }

            return isThisTime;
        }

        private void radioButton_Click(object sender, EventArgs e)
        {
            var button = sender as RadioButton;
            DateTime now = DateTime.Now;
            DateTime startDate, endDate;
            int year;

            if (button != null)
            {
                var buttonName = button.Text;
                MountEnum selectedMonth;

                if (Enum.TryParse(buttonName, out selectedMonth))
                    Console.WriteLine("Успешно преобразовано имя месяца " + selectedMonth );
                else
                    Console.WriteLine("Ошибка преобразования месяца");

                switch (selectedMonth)
                {
                    case MountEnum.Январь:
                        year = ReturnStartEndDate(1, out startDate, out endDate);
                        break;
                    case MountEnum.Февраль:
                        year = ReturnStartEndDate(2, out startDate, out endDate);
                        break;
                    case MountEnum.Март:
                        year = ReturnStartEndDate(3, out startDate, out endDate);
                        break;
                    case MountEnum.Апрель:
                        year = ReturnStartEndDate(4, out startDate, out endDate);
                        break;
                    case MountEnum.Май:
                        year = ReturnStartEndDate(5, out startDate, out endDate);
                        break;
                    case MountEnum.Июнь:
                        year = ReturnStartEndDate(6, out startDate, out endDate);
                        break;
                    case MountEnum.Июль:
                        year = ReturnStartEndDate(7, out startDate, out endDate);
                        break;
                    case MountEnum.Август:
                        year = ReturnStartEndDate(8, out startDate, out endDate);
                        break;
                    case MountEnum.Сентабрь:
                        year = ReturnStartEndDate(9, out startDate, out endDate);
                        break;
                    case MountEnum.Октябрь:
                        year = ReturnStartEndDate(10, out startDate, out endDate);
                        break;
                    case MountEnum.Ноябрь:
                        year = ReturnStartEndDate(11, out startDate, out endDate);
                        break;
                    case MountEnum.Декабрь:
                        year = ReturnStartEndDate(12, out startDate, out endDate);
                        break;
                    default:
                        year = ReturnStartEndDate(12, out startDate, out endDate);
                        Console.WriteLine("Ошибка месяца");
                        break;
                }

                ChangeViewDataGridView(startDate, endDate, year);
            }
        }

        private int ReturnStartEndDate(int mount, out DateTime startDate, out DateTime endDate)
        {
            int year;

            if (int.TryParse(numericUpDown1.Value.ToString(), out year))
                Console.WriteLine("Успешно преобразов год " + year);
            else
                Console.WriteLine("Ошибка преобразования года");

            startDate = new DateTime(year, mount, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);

            return year;
        }

        private void button_find_Click(object sender, EventArgs e)
        {
            int yaer;
            if(int.TryParse(numericUpDown1.Value.ToString(), out yaer))
            {
                ChangeViewDataGridView(dateTimePicker_from.Value, dateTimePicker_before.Value, yaer);
            }
            else
            {
                MessageBox.Show("Ошибка поиска в этом промежутке времени");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;

            switch (comboBox.SelectedIndex)
            {
                case 0:
                    if (pieChart1.Visible == true)
                        pieChart1.Visible = false;

                    if (dataGridView1.Visible == false)
                        dataGridView1.Visible = true;

                    break;
                case 1:
                    
                    if(pieChart1.Visible == false)
                        pieChart1.Visible = true;

                    if(dataGridView1.Visible == true)
                        dataGridView1.Visible = false;

                    break;
                case 2:
                    if (pieChart1.Visible == true)
                        pieChart1.Visible = false;

                    if(dataGridView1.Visible == true)
                        dataGridView1.Visible = false;

                    break;
                default:
                    break;
            }
        }

        private void ChangeDiagram()
        {
            SeriesCollection series = new SeriesCollection();

            for (int i = 0; i < _analyzer.GetNumberTypesDowntime(); i++)
            {
                var downtime = _analyzer.GetCountTypeDowntime(i);
                var seconds = downtime.TotalSeconds; // Преобразование TimeSpan в секунды

                Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
                Random rnd = new Random();
                var pieSeries = new PieSeries
                {
                    Values = new ChartValues<double> { seconds },
                    Title = _analyzer.GetNameTypeDowntime(i),
                    DataLabels = true,
                    LabelPoint = labelPoint,
                };

                series.Add(pieSeries);
            }
            pieChart1.LegendLocation = LegendLocation.Right;
            pieChart1.Series = series;
        }
    }
}

