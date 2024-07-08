using Downtime_table;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {  
        private int _minusDifferenceHour = 0;
        private int _minusDifferenceMinut = 10;
        private int _minusDifferenceSecond = 30;
        private DataSet _dsMain;
        private DataSet _dsIdle;
        public List<Date> datesNew = new List<Date>();
        private List<Date> datesPast = new List<Date>();
        private List<string> _comments;
        private List<string> _recepts;
        private List<newDate> newDates = new List<newDate>();
        private bool isNewData;
        List<DateIdle> _idles;
        private List<Recept> _LocalPCRecepts = new List<Recept>();
        private List<Recept> _ServerRecepts = new List<Recept>();

        private string _errorOldBdMessage = "Unknown system variable 'lower_case_table_names'";

        public async void GetMain(DateTime dateTime, DataGridView dataGridView1)
        {
            //Обновляет данные из локольной базы пк на сервер для получения recepts
            _ServerRecepts = await GetServerRecepts();
            _LocalPCRecepts = await GetLocalPCRecepts();
            ChecksDataDifferenceRecepts();

            DataSet ds = new DataSet();
            string sql, sqlLastData;
            DateTime currentTime = dateTime;
            DateTime nextData = currentTime.AddDays(1);
            DateTime lastDate = currentTime.AddDays(-1);
            TimeSpan timeOfDay = currentTime.TimeOfDay;

            _idles = await GetIdlesAsync();
            

            if (currentTime.TimeOfDay >= new TimeSpan(8, 30, 0) && currentTime.TimeOfDay < new TimeSpan(20, 29, 0))
            {
                sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'";
                sqlLastData = $"select * from downTime where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 08:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:00:00'";
            }
            else
            {
                if(currentTime.TimeOfDay <= new TimeSpan(24, 59, 59) && currentTime.TimeOfDay >= new TimeSpan(20, 00, 00))
                {
                    sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlLastData = $"select * from downTime where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00'";
                }
                else if(currentTime.TimeOfDay <= new TimeSpan(8, 29, 00))
                {
                    sql = $"SELECT DBID, Timestamp FROM spslogger.mixreport where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlLastData = $"select * from downTime where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00'";

                }
                else
                {
                    sql = null;
                    sqlLastData = null;
                    throw new Exception("Ошибка времени");
                }
            }
                
            datesPast = await ReturnLastDataAsync(sqlLastData);
            datesNew = await ReturnDataAsync(sql);

            datesNew = CalculateDowntime(newDates, new TimeSpan(_minusDifferenceHour, _minusDifferenceMinut,_minusDifferenceSecond));

            _dsMain = DeletesIdenticalData(ds, dsPast);


            dataGridView1.DataSource = _dsMain;
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

        private async Task<List<Date>> ReturnLastDataAsync(string query)
        {
            List<Date> datesLocal = new List<Date>();

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();
                    }
                    catch (MySqlException ex)
                    {
                        if(ex.Message == _errorOldBdMessage)
                            goto Select;
                        else
                            MessageBox.Show(ex.Message);
                    }

                Select:

                    using (MySqlCommand command = new MySqlCommand(query, connection))
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
                                        reader.GetString(5)));
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
                    await connection.CloseAsync();
                }
            }
            return datesLocal;
        }

        private async Task<List<Date>> ReturnDataAsync(string query)
        {
            List<Date> datesLocal = new List<Date>();

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        if (connection.State == ConnectionState.Closed)
                            await connection.OpenAsync();
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Message == _errorOldBdMessage)
                            goto Select;
                        else
                            MessageBox.Show(ex.Message);
                    }

                Select:

                    using (MySqlCommand command = new MySqlCommand(query, connection))
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            return datesLocal;
        }

        public async Task<List<DateIdle>> GetIdlesAsync()
        {
            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Message == _errorOldBdMessage)
                        {
                            goto Select;
                        }
                        else
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                Select:
                    string query = "SELECT * FROM spslogger.ididles;";
                    List<DateIdle> idles = new List<DateIdle>();

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
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
                finally { await connection.CloseAsync(); }
            }
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

        public async Task<List<Recept>> GetRecept()
        {
            string query = "SELECT Name, Time FROM spslogger.recepttime";

            List<Recept> recept = new List<Recept>();

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recept.Add(new Recept(reader.GetString(0)));
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally { await connection.CloseAsync(); }
            }
            return recept;
        }

        public async Task<string[]> GetCommentsAsync()
        {
            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Message == _errorOldBdMessage)
                        {
                            goto Select;
                        }
                        else
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                Select:
                    string query = "SELECT Comment FROM downtime group by Comment";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            _comments = new List<string>();
                            while (await reader.ReadAsync())
                            {
                                _comments.Add(reader.GetString(0));
                            }
                            reader.Close();
                        }
                    }
                    return _comments.ToArray();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally { await connection.CloseAsync(); }
            }
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

        private List<Database> DeletesIdenticalData()
        {
            List<Database> result = new List<Database>();

            int count = 0;

            foreach (var dateNew in datesNew)
            {
                foreach(var datePast in datesPast)
                {
                    if (dateNew
                }
            }

            return result;
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

        public void ClearData()
        {
            newDates.Clear();
            datesPast.Clear();
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

        private async Task<List<Recept>> GetLocalPCRecepts()
        {
            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch(MySqlException ex)
                    {
                        if(ex.Message == _errorOldBdMessage)
                        {
                            goto Select;
                        }
                        else
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                Select:

                    string query = "SELECT recepte FROM spslogger.error_mas group by recepte;";

                    List<Recept> recepts = new List<Recept>();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recepts.Add(new Recept(reader.GetString(0)));
                            }
                            reader.Close();
                        }
                        return recepts;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally { await connection.CloseAsync(); }
            }
            return null;
        }

        private async Task<List<Recept>> GetServerRecepts()
        {
            string query = "SELECT Name FROM spslogger.receptTime group by Name;";

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    List<Recept> recepts = new List<Recept>();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recepts.Add(new Recept(reader.GetString(0)));
                            }
                            reader.Close();
                        }
                        return recepts;
                    }
                }
                catch(MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { await connection.CloseAsync(); }
            }
            
            return null;
        }

        private void ChecksDataDifferenceRecepts()
        {
            if (_LocalPCRecepts != null && _ServerRecepts != null)
            {
                List<Recept> recepts = _LocalPCRecepts.Where(r1 => !_ServerRecepts.Any(r2 => r2.Name == r1.Name)).ToList();

                if (recepts.Count >= 1)
                {
                    ChangeDBReceptTime(recepts);
                }
                else
                {
                    Console.WriteLine("Данные идентичны");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Ошибка в обновлении рецептов");
            }
        }

        private async void ChangeDBReceptTime(List<Recept> recepts)
        {
            string sqlInsert = "INSERT INTO spslogger.recepttime (Name, Time) VALUES (@Name, @Time)";

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(sqlInsert, connection))
                    {
                        command.Parameters.Add("@Name", MySqlDbType.VarChar);
                        command.Parameters.Add("@Time", MySqlDbType.Time);

                        foreach (Recept recept in recepts)
                        {
                            command.Parameters["@Name"].Value = recept.Name;
                            command.Parameters["@Time"].Value = recept.Time;
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                
                catch (MySqlException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { await connection.CloseAsync(); }
            }
        }

        /// <summary>
        /// Возвращает LIST DateIdle
        /// </summary>
        /// <returns>LIST DateIdle or null</returns>
        public List<DateIdle> GetIdles()
        {
            if (_idles != null && _idles.Count > 0)
                return _idles;
            else
                return null;
        }
    }
}
