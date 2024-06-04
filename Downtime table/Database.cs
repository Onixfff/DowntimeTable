﻿using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);
        private DataSet _dsMain;
        private DataSet _dsIdle;
        public List<Date> datesNew = new List<Date>();
        private List<Date> datesPast = new List<Date>();
        private List<string> comments;
        private bool isNewData;
        List<DateIdle> idles;

        public async Task<DataSet> GetMain(DateTime dateTime, DataGridView dataGridView1)
        {
            DataSet ds = new DataSet();
            string sql, sqlDownTime;
            DateTime currentTime = dateTime;
            DateTime nextData = currentTime.AddDays(1);
            DateTime lastDate = currentTime.AddDays(-1);
            TimeSpan timeOfDay = currentTime.TimeOfDay;
            idles = await GetIdles();
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
            if (currentTime.TimeOfDay >= TimeSpan.FromHours(8) && currentTime.TimeOfDay < TimeSpan.FromHours(20))
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'), downtime AS ( SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";
                sqlDownTime = $"select * from downTime where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'";
            }
            else if (timeOfDay >= TimeSpan.FromHours(20) && nextData.TimeOfDay < TimeSpan.FromHours(8))
            {
                sql = $"with TimeSampling as (SELECT * FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' or Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00'),downtime AS (SELECT t1.DBID, t1.timestamp, timediff( TIMEDIFF(t2.timestamp, t1.timestamp), '00:07:30') as \"Разница\" FROM (SELECT *, LEAD(DBID) OVER (ORDER BY DBID) AS next_DBID FROM TimeSampling ) t1 JOIN TimeSampling t2 ON t1.next_DBID = t2.DBID WHERE TIMEDIFF(t2.timestamp, t1.timestamp) > '00:07:30') select * from downtime;";
                sqlDownTime = "select * from downTime where Timestamp >= '{currentTime.ToString(\"yyyy-MM-dd\")} 20:00:00' or Timestamp < '{nextData.ToString(\"yyyy-MM-dd\")} 08:00:00'";
            }
            else
            {
                throw new Exception("Ошибка в интервале времени");
            }
#endif
            try
            {

                DataTable dtNew = new DataTable("MyTable");

                dtNew.Columns.Add(new DataColumn("id", typeof(int)));
                dtNew.Columns[0].ReadOnly = true;
                dtNew.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                dtNew.Columns[1].ReadOnly = true;
                dtNew.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                dtNew.Columns[2].ReadOnly = true;
                dtNew.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                datesNew.Clear();
                datesNew = await CheckData(sqlDownTime);

                DataTable dtPast = new DataTable("MyTable");

                dtPast.Columns.Add(new DataColumn("id", typeof(int)));
                dtPast.Columns[0].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                dtPast.Columns[1].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                dtPast.Columns[2].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                datesPast.Clear();
                datesPast = await CheckData(sqlDownTime);

                //if(dates != null && dates.Count > 0)
                //{
                //    isNewData = false;
                //}
                //else
                //{
                //    isNewData = true;
                //}

                try
                {
                    await _mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Select;
                }

            Select:
                //if (isNewData)
                //{
                datesNew.Clear();

                using (MySqlCommand command = new MySqlCommand(sql, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            datesNew.Add(new Date(
                                    reader.GetInt32(0),
                                    reader.GetDateTime(1),
                                    reader.GetTimeSpan(2),
                                    false));
                        }
                        reader.Close();
                    }
                }
                //}

                for (int i = 0; i < datesNew.Count; i++)
                {
                    DataRow dr = dtNew.NewRow();
                    dr["id"] = datesNew[i].Id;
                    dr["Время начала"] = datesNew[i].Timestamp;
                    dr["Время простоя"] = datesNew[i].Difference;
                    dr["Комментарий"] = datesNew[i].Comments;
                    dtNew.Rows.Add(dr);
                }

                for (int i = 0; i < datesPast.Count; i++)
                {
                    DataRow dr = dtPast.NewRow();
                    dr["id"] = datesPast[i].Id;
                    dr["Время начала"] = datesPast[i].Timestamp;
                    dr["Время простоя"] = datesPast[i].Difference;
                    dr["Комментарий"] = datesPast[i].Comments;
                    dtPast.Rows.Add(dr);
                }

                DataSet dsPast = new DataSet();

                ds.Tables.Add(dtNew);
                dsPast.Tables.Add(dtPast);
                //_dsMain = ds;

                _dsMain = DeletesIdenticalData(ds, dsPast);


                dataGridView1.DataSource = ds;
                return _dsMain;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }

            return null;
        }

        public async Task<List<Date>> CheckData(string query)
        {
            List<Date> datesLocal = new List<Date>();
            try
            {

                try
                {
                    if (_mCon.State == ConnectionState.Closed)
                        await _mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Select;
                }

            Select:
                using (MySqlCommand command = new MySqlCommand(query, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            datesLocal.Add(new Date(
                                    reader.GetInt32(0),
                                    reader.GetDateTime(1),
                                    reader.GetTimeSpan(2),
                                    reader.GetInt32(3),
                                    reader.GetString(4),
                                    true));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await _mCon.CloseAsync();
            }

            return datesLocal;
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }

            return null;
        }

        public void ChangeData(int id, int idIdle)
        {

            for (int i = 0; i < datesNew.Count; i++)
            {
                if (datesNew[i].Id == id)
                {
                    datesNew[i].IdTypeDowntime = idIdle;
                }
            }
        }

        public void ChangeData(int id, string comment)
        {

            for (int i = 0; i < datesNew.Count; i++)
            {
                if (datesNew[i].Id == id)
                {
                    datesNew[i].Comments = comment;
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
                catch (MySqlException ex)
                {
                    goto Insert;
                }

            Insert:
                var query = new System.Text.StringBuilder("INSERT INTO downtime (Timestamp, Difference, idIdle , Comment) VALUES ");

                // Добавление всех значений в запрос
                var valueList = new List<string>();
                foreach (var entry in datesNew)
                {
                    if (entry.IsUpdate == true)
                    {
                        valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', '{entry.Difference}', '{entry.IdTypeDowntime}', '{entry.Comments.Replace("'", "''")}')");
                    }
                }

                // Соединяем все строки значений в один запрос
                query.Append(string.Join(", ", valueList));
                query.Append(";");

                // Создаем команду и выполняем запрос
                using (MySqlCommand cmd = new MySqlCommand(query.ToString(), _mCon))
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Данные записаны");
                    isNewData = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }

            UpdateAsync(datesPast);
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

        public bool GetBoolIsNewData()
        {
            return isNewData;
        }

        public List<Date> GetListDate()
        {
            return datesPast;
        }

        public async void UpdateAsync(List<Date> datesNew)
        {
            try
            {
                try
                {
                    await _mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Update;
                }

            Update:
                string query = "Update downtime set ";
                query += "idIdle = case ";
                
                for(int i = 0; i < datesNew.Count; i++)
                {
                    query += $" WHEN Id = {datesNew[i].Id} THEN {datesNew[i].IdTypeDowntime}";
                }

                query += " else idIdle END,";

                query += "Comment = case ";

                for (int i = 0; i < datesNew.Count; i++)
                {
                    query += $" WHEN Id = {datesNew[i].Id} THEN '{datesNew[i].Comments}'";
                }
                
                query += " else Comment END ";

                query += "Where id IN(";
                
                for (int i = 0 ; i < datesNew.Count; i++)
                {
                    if(i < datesNew.Count - 1)
                    {
                        query += $"{datesNew[i].Id},";
                    }
                    else
                    {
                        query += $"{datesNew[i].Id}";
                    }
                }
                
                query += ");";



                // Создаем команду и выполняем запрос
                using (MySqlCommand cmd = new MySqlCommand(query.ToString(), _mCon))
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Данные обновлены");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }
        }

        private DataSet DeletesIdenticalData(DataSet datasetNew, DataSet datasetPast)
        {
            var table1 = datasetNew.Tables[0];
            var table2 = datasetPast.Tables[0];
            DataTable resultTable = table1.Clone(); // Клонируем структуру таблицы table1
            DataSet resultDataSet = new DataSet();

            foreach (DataRow row1 in table1.Rows)
            {
                DateTime column1Value = (DateTime)row1["Время начала"];
                string filterExpression = $"[Время начала] = #{column1Value:yyyy-MM-dd HH:mm:ss}#";
                DataRow[] matchingRows = table2.Select(filterExpression);

                if (matchingRows.Length > 0)
                {
                    // Если совпадающая строка найдена в table2, берем первую строку из matchingRows
                    DataRow row2 = matchingRows[0];
                    resultTable.ImportRow(row2);
                }
                else
                {
                    // Если совпадающая строка не найдена в table2, берем row1 из table1
                    resultTable.ImportRow(row1);
                }
            }
            // Добавляем результирующую таблицу в DataSet
            resultDataSet.Tables.Add(resultTable);
            return resultDataSet;
        }
    }
}
