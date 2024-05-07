using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Downtime_table
{
    public class Database
    {
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);
        private DataSet _dsMain;
        private DataSet _dsIdle;
        List<Date> dates = new List<Date>();

        public async Task<DataSet> GetMain(DateTime dateTime)
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
                dt.Columns.Add(new DataColumn("Вид простоя", typeof(string)));
                dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                for (int i = 0; i < dates.Count; i++)
                {
                    DataRow dr = dt.NewRow();
                    dr["id"] = dates[i].Id;
                    dr["Время начала"] = dates[i].Timestamp;
                    dt.Rows.Add(dr);
                }

                ds.Tables.Add(dt);
                _dsMain = ds;
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally {await _mCon.CloseAsync(); }

            return null;
        }

        public async Task<DataSet> GetIdles()
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
                string query = "";
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

                        DataTable dt = new DataTable("MyTable");

                        dt.Columns.Add(new DataColumn("id", typeof(int)));
                        dt.Columns[0].ReadOnly = true;
                        dt.Columns.Add(new DataColumn("Наименование", typeof(string)));
                        dt.Columns[1].ReadOnly = true;

                        for (int i = 0; i < idles.Count; i++)
                        {
                            DataRow dr = dt.NewRow();
                            dr["id"] = idles[i].Id;
                            dr["Наименование"] = idles[i].Name;
                            dt.Rows.Add(dr);
                        }

                        _dsIdle.Tables.Add(dt);
                        return _dsIdle;
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

        public DataSet ChangeData(int id, string idle, int idIdle)
        {

            for (int i = 0; i < dates.Count; i++)
            {
                if (dates[i].Id == id)
                {
                    dates[i].IdTypeDowntime = idIdle;
                    dates[i].TypeDowntime = idle;
                }
            }

            DataTable dt = new DataTable("MyTable");

            dt.Columns.Add(new DataColumn("id", typeof(int)));
            dt.Columns[0].ReadOnly = true;
            dt.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
            dt.Columns[1].ReadOnly = true;
            dt.Columns.Add(new DataColumn("Вид простоя", typeof(string)));
            dt.Columns.Add(new DataColumn("Комментарий", typeof(string)));

            for (int i = 0; i < dates.Count; i++)
            {
                DataRow dr = dt.NewRow();
                dr["id"] = dates[i].Id;
                dr["Время начала"] = dates[i].Timestamp;
                dt.Rows.Add(dr);
            }

            _dsMain.Tables.Add(dt);
            return _dsMain;
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
                    valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', {entry.Difference}, '{entry.IdTypeDowntime}' '{entry.Comments.Replace("'", "''")}')");
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


    }
}
