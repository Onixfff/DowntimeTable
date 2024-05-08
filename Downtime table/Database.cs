using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static Downtime_table.Form1;

namespace Downtime_table
{
    public class Database
    {
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);
        private DataSet _dsMain;
        private DataSet _dsIdle;
        public List<Date> dates = new List<Date>();
        private List<string> comments;

        public async Task<DataSet> GetMain(DateTime dateTime, DataGridView dataGridView1)
        {
            DataSet ds = new DataSet();
            string sql;
            DateTime currentTime = dateTime;
            DateTime nextData = currentTime.AddDays(1);
            DateTime lastDate = currentTime.AddDays(-1);
            TimeSpan timeOfDay = currentTime.TimeOfDay;
#if (!DEBUG)
            if ((timeOfDay >= TimeSpan.FromHours(8) && timeOfDay < TimeSpan.FromHours(20)) ||
                (timeOfDay >= TimeSpan.FromHours(20) || timeOfDay < TimeSpan.FromHours(8)))
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '2014-12-04 08:00:00' and Timestamp < '2014-12-04 20:00:00'), downtime AS ( SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";

            }
            else
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '2014-12-04 20:00:00' or Timestamp < '2014-12-05 08:00:00'),downtime AS (SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";

            }
#else
            if ((timeOfDay >= TimeSpan.FromHours(8) && timeOfDay < TimeSpan.FromHours(20)) ||
                (timeOfDay >= TimeSpan.FromHours(20) || timeOfDay < TimeSpan.FromHours(8)))
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{lastDate.ToString("yyyy-MM-dd")} 20:00:00'), downtime AS ( SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";
            }
            else
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' or Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00'),downtime AS (SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";
            }
#endif
            try
            {
                try
                {
                    await _mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Select;
                }

            Select:

                using (MySqlCommand command = new MySqlCommand(sql, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dates.Add(new Date(
                                    reader.GetInt32(0),
                                    reader.GetDateTime(1),
                                    reader.GetTimeSpan(2)));
                        }
                        reader.Close();
                    }
                }

                DataTable dt = new DataTable("MyTable");

                dt.Columns.Add(new DataColumn("id", typeof(int)));
                dt.Columns[0].ReadOnly = true;
                dt.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                dt.Columns[1].ReadOnly = true;
                dt.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                dt.Columns[2].ReadOnly = true;
                dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                for (int i = 0; i < dates.Count; i++)
                {
                    DataRow dr = dt.NewRow();
                    dr["id"] = dates[i].Id;
                    dr["Время начала"] = dates[i].Timestamp;
                    dr["Время простоя"] = dates[i].Difference;
                    dr["Комментарий"] = dates[i].Comments;
                    dt.Rows.Add(dr);
                }

                ds.Tables.Add(dt);
                _dsMain = ds;

                dataGridView1.DataSource = ds;
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally {await _mCon.CloseAsync(); }

            return null;
        }

        public async Task<List<DateIdle>> GetIdles()
        {
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
                string query = "SELECT * FROM spslogger.ididles;";
                List<DateIdle> idles = new List<DateIdle>();

                using (MySqlCommand cmd = new MySqlCommand(query, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            idles.Add(
                                new DateIdle
                                (
                                    reader.GetInt32(0),
                                    reader.GetString(1)
                                )
                            );
                        }
                        reader.Close();
                        return idles;
                    }
                }

            }
            catch(Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }
            
            return null;
        }

        public void ChangeData(int id, int idIdle)
        {

            for (int i = 0; i < dates.Count; i++)
            {
                if (dates[i].Id == id)
                {
                    dates[i].IdTypeDowntime = idIdle;
                }
            }
        }

        public void ChangeData(int id, string comment)
        {

            for(int i = 0; i < dates.Count; i++)
            {
                if (dates[i].Id == id)
                {
                    dates[i].Comments = comment;
                }
            }
        }

        public async void InsertData()
        {
            try
            {
                try
                {
                    await _mCon.OpenAsync();
                }
                catch(MySqlException ex)
                {
                    goto Insert;
                }

            Insert:
                var query = new System.Text.StringBuilder("INSERT INTO downtime (Timestamp, Difference, idIdle , Comment) VALUES ");

                // Добавление всех значений в запрос
                var valueList = new List<string>();
                foreach (var entry in dates)
                {
                    valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', { entry.Difference}, '{entry.IdTypeDowntime}' '{entry.Comments.Replace("'", "''")}')");
                }

                // Соединяем все строки значений в один запрос
                query.Append(string.Join(", ", valueList));
                query.Append(";");

                // Создаем команду и выполняем запрос
                using (MySqlCommand cmd = new MySqlCommand(query.ToString(), _mCon))
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Данные записаны");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }
        }

        public async Task<string[]> GetComments()
        {
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
                string query = "SELECT Comment FROM downtime group by Comment";

                using (MySqlCommand command = new MySqlCommand(query, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        comments = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            comments.Add(reader.GetString(0));
                        }
                        reader.Close();
                    }
                }
                return comments.ToArray();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync();}
            return null;
        }

    }
}
