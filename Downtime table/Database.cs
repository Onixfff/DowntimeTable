using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);

        public Database()
        {
            
        }

        public async Task<DataSet> GetData(DateTime dateTime)
        {
            DataSet ds = new DataSet();
            string sql;
            DateTime currentTime = dateTime;
            DateTime nextData = currentTime.AddDays(1);
            TimeSpan timeOfDay = currentTime.TimeOfDay;
#if (DEBUG)
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
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'), downtime AS ( SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";
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
                catch (MySqlException ex)
                {
                    goto Select;
                }

            Select:
                List<Date> dates = new List<Date>();

                    using (MySqlCommand command = new MySqlCommand(sql, _mCon))
                    {

                        using (MySqlDataReader reader = (MySqlDataReader) await command.ExecuteReaderAsync())
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

                    DataTable dt = new DataTable("MyTable");

                    dt.Columns.Add(new DataColumn("id", typeof(int)));
                    dt.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                    dt.Columns.Add(new DataColumn("Время простоя", typeof(DateTime)));
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally {await _mCon.CloseAsync(); }

            return ds;
        }
    }
}
