using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {
        private MySqlConnection _mCon = new MySqlConnection(ConfigurationManager.ConnectionStrings["server"].ConnectionString);
        private MySqlConnection _mConLocal = new MySqlConnection(ConfigurationManager.ConnectionStrings["dbLocalServer"].ConnectionString);
        private int _minusDifferenceHour = 0;
        private int _minusDifferenceMinut = 10;
        private int _minusDifferenceSecond = 00;
        private DataSet _dsMain;
        private DataSet _dsIdle;
        public List<Date> datesNew = new List<Date>();
        private List<Date> datesPast = new List<Date>();
        private List<string> comments;
        private List<newDate> newDates = new List<newDate>();
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
            idles = await GetIdles(_mConLocal);

            if (currentTime.TimeOfDay >= new TimeSpan(8, 30, 0) && currentTime.TimeOfDay < new TimeSpan(20, 29, 0))
            {
                sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'";
                sqlDownTime = $"select * from downTime where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'";
            }
            else
            {
                if(currentTime.TimeOfDay <= new TimeSpan(24, 59, 59) && currentTime.TimeOfDay >= new TimeSpan(20, 00, 00))
                {
                    sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlDownTime = $"select * from downTime where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00'";
                }
                else if(currentTime.TimeOfDay <= new TimeSpan(8, 29, 00))
                {
                    sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlDownTime = $"select * from downTime where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00'";

                }
                else
                {
                    throw new Exception("Ошибка промежутка времени");
                    sql = null;
                    sqlDownTime = null;
                }
            }
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

                DataTable dtPast = new DataTable("MyTable");

                dtPast.Columns.Add(new DataColumn("id", typeof(int)));
                dtPast.Columns[0].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Время начала", typeof(DateTime)));
                dtPast.Columns[1].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Время простоя", typeof(TimeSpan)));
                dtPast.Columns[2].ReadOnly = true;
                dtPast.Columns.Add(new DataColumn("Комментарий", typeof(string)));

                datesPast.Clear();
                datesPast = await CheckData(sqlDownTime, _mConLocal);

                try
                {
                    await _mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Select;
                }

            Select:

                datesNew.Clear();

                if (_mCon.State != ConnectionState.Open)
                    throw new Exception("Ошибка получения данных");

                using (MySqlCommand command = new MySqlCommand(sql, _mCon))
                {
                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            newDates.Add(new newDate
                                (
                                reader.GetInt32(0),
                                reader.GetDateTime(1)
                                ));
                        }
                        reader.Close();
                    }
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

                datesNew = CalculateDowntime(newDates, new TimeSpan(_minusDifferenceHour, _minusDifferenceMinut,_minusDifferenceSecond));
                ds.Clear();
                dsPast.Tables.Add(dtPast);

                for (int i = 0; i < datesNew.Count; i++)
                {
                    DataRow dr = dtNew.NewRow();
                    dr["id"] = datesNew[i].Id;
                    dr["Время начала"] = datesNew[i].Timestamp;
                    dr["Время простоя"] = datesNew[i].Difference;
                    dr["Комментарий"] = datesNew[i].Comments;
                    dtNew.Rows.Add(dr);
                }
                ds.Tables.Add(dtNew);

                _dsMain = DeletesIdenticalData(ds, dsPast);


                dataGridView1.DataSource = _dsMain;
                return _dsMain;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { await _mCon.CloseAsync(); }

            return null;
        }

        private List<Date> CalculateDowntime(List<newDate> newDate, TimeSpan difference)
        {
            List<Date> datesNew = new List<Date>();
            TimeSpan timeSpan = new TimeSpan(_minusDifferenceHour, _minusDifferenceMinut, _minusDifferenceSecond);

            for (int i = 0; i < newDate.Count -1; i++)
            {
                var dt = newDate[i];
                var nextDt = newDate[i+1];
                var result =  nextDt.DateTime - dt.DateTime;

                if (result.TotalSeconds >= difference.TotalSeconds)
                {
                    datesNew.Add(new Date(dt.DBIG, dt.DateTime, result - timeSpan));
                }
            }

            return datesNew;
        }

        public async Task<List<Date>> CheckData(string query, MySqlConnection mCon)
        {
            List<Date> datesLocal = new List<Date>();
            try
            {

                try
                {
                    if (mCon.State == ConnectionState.Closed)
                        await mCon.OpenAsync();
                }
                catch (MySqlException)
                {
                    goto Select;
                }

            Select:

                using (MySqlCommand command = new MySqlCommand(query, mCon))
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
                                    reader.GetString(4)));
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
                await mCon.CloseAsync();
            }

            return datesLocal;
        }

        public async Task<List<DateIdle>> GetIdles(MySqlConnection _mCon)
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

            for(int i = 0; i < datesPast.Count; i++)
            {
                if (datesPast[i].Id == id)
                {
                    datesPast[i].IdTypeDowntime = idIdle;
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

            for (int i = 0; i < datesPast.Count; i++)
            {
                if (datesPast[i].Id == id)
                {
                    datesPast[i].Comments = comment;
                }
            }
        }

        public async void InsertData(MySqlConnection _mCon)
        {
            try
            {
                try
                {
                    switch (_mCon.State)
                    {
                        case ConnectionState.Closed:
                            await _mCon.OpenAsync();
                            break;
                        case ConnectionState.Open:
                            break;
                        case ConnectionState.Connecting:
                            Thread.Sleep(1000);
                            if (_mCon.State != ConnectionState.Open)
                            {
                                throw new Exception();
                            }
                            break;
                        case ConnectionState.Executing:
                            break;
                        case ConnectionState.Fetching:
                            break;
                        case ConnectionState.Broken:
                            break;
                    }
                }
                catch (MySqlException)
                {
                    goto Insert;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения\n\n", ex.Message);
                }

            Insert:
                var query = new System.Text.StringBuilder("INSERT INTO downtime (Timestamp, Difference, idIdle , Comment) VALUES ");

                // Добавление всех значений в запрос
                var valueList = new List<string>();
                int countInt = 0;
                foreach (var entry in datesNew)
                {
                    if (entry._isPastData == false)
                    {
                        valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', '{entry.Difference}', '{entry.IdTypeDowntime}', '{entry.Comments.Replace("'", "''")}')");
                        countInt++;
                    }
                }

                // Соединяем все строки значений в один запрос
                query.Append(string.Join(", ", valueList));
                query.Append(";");

                string queryUpdate = GetUpdateQuery(datesPast);
                bool isComplite = false;
                if (countInt > 0 && queryUpdate != null)
                {
                    var queryFull = query.ToString() + queryUpdate;
                    // Создаем команду и выполняем запрос
                    using (MySqlCommand cmd = new MySqlCommand(queryFull, _mCon))
                    {
                        cmd.ExecuteNonQuery();
                        isComplite = true;
                    }
                }
                else if(countInt > 0 && queryUpdate == null)
                {
                    // Создаем команду и выполняем запрос
                    using (MySqlCommand cmd = new MySqlCommand(query.ToString(), _mCon))
                    {
                        cmd.ExecuteNonQuery();
                        isComplite = true;
                    }
                }
                else if(countInt <= 0 && queryUpdate != null)
                {
                    // Создаем команду и выполняем запрос
                    using (MySqlCommand cmd = new MySqlCommand(queryUpdate, _mCon))
                    {
                        cmd.ExecuteNonQuery();
                        isComplite = true;
                    }
                }

                ClearData();

                if(isComplite)
                    MessageBox.Show("Данные сохранены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally 
            { 
                await _mCon.CloseAsync();
            }

        }

        public async Task<string[]> GetComments(MySqlConnection _mCon)
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

        /// <summary>
        /// Возвращает string sql для обновления
        /// </summary>
        /// <param name="datesNew">Данные для обновления</param>
        /// <returns>if(datesNew.Count <= 0) return null; else return string sql</returns>
        public string GetUpdateQuery(List<Date> datesNew)
        {
            string query = "Update downtime set ";
            query += "idIdle = case ";
                
            if(datesNew.Count <= 0)
            {
                return null;
            }

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
            return query;
        }

        private DataSet DeletesIdenticalData(DataSet datasetNew, DataSet datasetPast)
        {
            var table1 = datasetNew.Tables[0];
            var table2 = datasetPast.Tables[0];
            DataTable resultTable = table1.Clone(); // Клонируем структуру таблицы table1
            DataSet resultDataSet = new DataSet();
            int FixedIdRemov = 0;
            for(int i = 0; i < table1.Rows.Count; i++)
            {
                DateTime column1Value = (DateTime)table1.Rows[i]["Время начала"];
                string filterExpression = $"[Время начала] = #{column1Value:yyyy-MM-dd HH:mm:ss}#";
                DataRow[] matchingRows = table2.Select(filterExpression);

                if (matchingRows.Length > 0)
                {
                    // Если совпадающая строка найдена в table2, берем первую строку из matchingRows
                    DataRow row2 = matchingRows[0];
                    resultTable.ImportRow(row2);
                    datesNew.RemoveAt(i - FixedIdRemov);
                    FixedIdRemov++;
                }
                else
                {
                    // Если совпадающая строка не найдена в table2, берем row1 из table1
                    resultTable.ImportRow(table1.Rows[i]);
                }
            }

            // Добавляем результирующую таблицу в DataSet
            resultDataSet.Tables.Add(resultTable);
            return resultDataSet;
        }

        public bool ChecksFieldsAreFilledIn()
        {
            bool checkDatesNew = true, checkDatesPast = true;

            for (int i = 0; i < datesNew.Count; i++)
            {
                if (datesNew[i].Comments != null)
                {
                    if (datesNew[i].IdTypeDowntime >= 0)
                    {
                        checkDatesNew = true;
                    }
                    else
                    {
                        checkDatesNew = false;
                        break;
                    }
                }
                else
                {
                    checkDatesNew = false;
                    break;
                }
            }

            for (int i = 0; i < datesPast.Count; i++)
            {
                if (datesPast[i].Comments != null)
                {
                    if (datesPast[i].IdTypeDowntime >= 0)
                    {
                        checkDatesPast = true;
                    }
                    else
                    {
                        checkDatesPast = false;
                        break;
                    }
                }
                else
                {
                    checkDatesPast = false;
                    break;
                }
            }


            if ( checkDatesPast  && checkDatesNew)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public TimeSpan GetDowntime()
        {
            var table = _dsMain.Tables[0];
            TimeSpan time = new TimeSpan();

            for(int i = 0; i < table.Rows.Count; i++)
            {
                TimeSpan column1Value = (TimeSpan)table.Rows[i]["Время простоя"];
                time += column1Value;
            }
            return time;
        }

        public void ClearData()
        {
            newDates.Clear();
            datesPast.Clear();
        }
    }
}
