﻿using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {  
        private DataSet _dsIdle;
        public List<Date> datesNew = new List<Date>();
        private List<Date> datesPast = new List<Date>();
        private List<string> _comments;
        private List<string> _recepts;
        private List<newDate> newDates = new List<newDate>();
        private bool isNewData;
        List<DateIdle> _idles = new List<DateIdle>();
        private List<Recept> _LocalPCRecepts = new List<Recept>();
        private List<Recept> _ServerRecepts = new List<Recept>();
        private List<Date> _resultDate = new List<Date>();
        private ILogger _logger;
        private ClassLibraryGetIp.Main _mainInstance = new ClassLibraryGetIp.Main();
        private string PcConnectionString, ServerConnectionString;

        private string _errorOldBdMessage = "Unknown system variable 'lower_case_table_names'";

        private Database(ILogger logger)
        {
            _logger = logger;
        }

        public static async Task<Database> CreateAsync(ILogger logger)
        {
            var db = new Database(logger);
            await db.updateDbConnection();
            return db;
        }

        private async Task updateDbConnection()
        {
            var PcConnectionString = await ChangeMconAsync("operator", ConfigurationManager.ConnectionStrings["Pc"].ConnectionString);
            var ServerConnectionString = await ChangeMconAsync("server", ConfigurationManager.ConnectionStrings["Server"].ConnectionString);

            if(PcConnectionString.error != null || ServerConnectionString.error != null)
            {
                string message = "Errro\n";

                if(PcConnectionString.error != null)
                {
                    message += $"PcConnection = {PcConnectionString.error}\n";
                }
                else if(ServerConnectionString.error != null)
                {
                    message += $"ServerConnection = {ServerConnectionString.error}\n";
                }
                throw new Exception(message);
            }
            else
            {
                this.PcConnectionString = PcConnectionString.updateConnection;
                this.ServerConnectionString = ServerConnectionString.updateConnection;
            }
        }

        public async Task<bool> GetMain(DateTime dateTime, DataGridView dataGridView1)
        {
            _logger.Trace("GetMain > Start");
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
                sql = $"SELECT DBID, Timestamp, Data_52 FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 07:30:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:29:00'";
                sqlLastData = $"select\r\nf1.Id, f1.Timestamp, f1.Difference, f2.Name, f2.Time, f1.idIdle, f3.name, f1.Comment\r\nfrom downTime as f1 \r\nleft join recepttime as f2 on f1.Recept = f2.Name \r\nleft join ididles as f3 on f1.idIdle = f3.name where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 07:30:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 20:29:00'";
            }
            else
            {
                if(currentTime.TimeOfDay <= new TimeSpan(24, 59, 59) && currentTime.TimeOfDay >= new TimeSpan(20, 00, 00))
                {
                    sql = $"SELECT DBID, Timestamp, Data_52 FROM spslogger.mixreport where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlLastData = $"select\r\nf1.Id, f1.Timestamp, f1.Difference, f2.Name, f2.Time, f1.idIdle, f3.name, f1.Comment\r\nfrom downTime as f1 \r\nleft join recepttime as f2 on f1.Recept = f2.Name \r\nleft join ididles as f3 on f1.idIdle = f3.name where Timestamp >= '{currentTime.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{nextData.ToString("yyyy-MM-dd")} 08:00:00'";
                }
                else if(currentTime.TimeOfDay <= new TimeSpan(8, 29, 00))
                {
                    sql = $"SELECT DBID, Timestamp, Data_52 FROM spslogger.mixreport where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00';";
                    sqlLastData = $"select\r\nf1.Id, f1.Timestamp, f1.Difference, f2.Name, f2.Time, f1.idIdle, f3.name, f1.Comment\r\nfrom downTime as f1 \r\nleft join recepttime as f2 on f1.Recept = f2.Name \r\nleft join ididles as f3 on f1.idIdle = f3.name where Timestamp >= '{lastDate.ToString("yyyy-MM-dd")} 20:00:00' and Timestamp < '{currentTime.ToString("yyyy-MM-dd")} 08:00:00'";

                }
                else
                {
                    sql = null;
                    sqlLastData = null;
                    throw new Exception("Ошибка времени");
                }
            }
                
            datesNew = await ReturnDataAsync(sql);
            datesPast = await ReturnLastDataAsync(sqlLastData);

            datesNew = CalculateDowntime(newDates);

            _resultDate = DeletesIdenticalData(ref datesNew, datesPast);
            return true;
        }

        public List<Date> GetDate()
        {
            if (_resultDate != null && _resultDate.Count > 0)
                return _resultDate;
            else
                Console.WriteLine("Нету данных");
                return _resultDate; 
        }

        private async Task<(string updateConnection, string error)> ChangeMconAsync(string nameIp, string _connectionString)
        {
            var ip = await _mainInstance.GetIp(nameIp);
            string error;

            try
            {
                if (ip.GetIp() != null)
                {
                    string updatedConnectionString = Regex.Replace(_connectionString, @"(?i)server=[^;]+", $"Server={ip.GetIp()}", RegexOptions.IgnoreCase);
                    return (updatedConnectionString, null);
                }
            }
            catch (InvalidOperationException)
            {
                error = "Не получилось соеденится с сервером. Попробуйте позже...";
                MessageBox.Show(error);
                return (null, error);
            }
            catch (Exception)
            {
                error = "Непредвиденная ошибка. Повторите попытку позже или свяжитесь с администратором";
                MessageBox.Show(error);
                return (null, error);
            }

            return (null, "Неизвестная ошибка.");
        }

        private List<Date> ChangeViewResult(Recept recept, List<Date> main)
        {
            List<Date> result = new List<Date>();

            foreach (var item in result)
            {
                if(item.Recept == recept)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private List<Date> CalculateDowntime(List<newDate> newDate)
        {
            List<Date> datesNew = new List<Date>();

            for (int i = 0; i < newDate.Count -1; i++)
            {

                string nameRecept = newDate[i].NameRecept;
                TimeSpan time;
                Recept recept = null;

                if (_ServerRecepts != null && _ServerRecepts.Count > 0)
                {
                    foreach (var item in _ServerRecepts)
                    {
                        if(item.Name == nameRecept)
                        {
                            recept = new Recept(nameRecept, item.Time);
                            break;
                        }
                    }

                    if(recept == null)
                        throw new Exception("Ошибка в нахождении похожего recept");
                }

                var dt = newDate[i];
                var nextDt = newDate[i+1];
                TimeSpan result = TimeSpan.Zero;
                
                if (dt.NameRecept == nextDt.NameRecept)
                {
                    result = nextDt.DateTime - dt.DateTime;
                    Console.WriteLine($"Начало {dt.DateTime} - {dt.NameRecept} сравнивается с {nextDt.DateTime} - {nextDt.NameRecept}");
                }

                TimeSpan timeSpan = recept.Time;

                if (result.TotalSeconds >= timeSpan.TotalSeconds)
                {
                    datesNew.Add(new Date(dt.DBIG, dt.DateTime, result - timeSpan, recept));
                }
            }

            return datesNew;
        }

        private async Task<List<Date>> ReturnLastDataAsync(string query)
        {
            List<Date> datesLocal = new List<Date>();

            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
                                Recept recept;
                                var name = reader.IsDBNull(3) ? null : reader.GetString(3);
                                TimeSpan? time = reader.IsDBNull(4) ? (TimeSpan?)null : reader.GetFieldValue<TimeSpan>(4);

                                if (time != null && name != null)
                                {
                                    recept = new Recept(name, time.Value);
                                }
                                else
                                {
                                    recept = new Recept("Не указано");
                                }

                                if (_ServerRecepts != null && _ServerRecepts.Count > 0)
                                {
                                    foreach (var item in _ServerRecepts)
                                    {
                                        string StringTypeDownTime, Comment;

                                        if (item.Name == name)
                                        {
                                            if(!await reader.IsDBNullAsync(6))
                                            {
                                                StringTypeDownTime = reader.GetString(6);
                                            }
                                            else
                                            {
                                                StringTypeDownTime = "Нету данных";
                                            }

                                            if (!await reader.IsDBNullAsync(7))
                                            {
                                                Comment = reader.GetString(7);
                                            }
                                            else
                                            {
                                                Comment = "Нету данных";
                                            }

                                            datesLocal.Add
                                                (new Date(
                                                    reader.GetInt32(0),
                                                    reader.GetDateTime(1),
                                                    reader.GetTimeSpan(2),
                                                    recept,
                                                    reader.GetInt32(5),
                                                    StringTypeDownTime,
                                                    Comment)
                                                );
                                        }
                                    }
                                }
                            }
                            reader.Close();
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "ReturnLastDataAsync > Error (TimeoutException ex)");
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

            using (MySqlConnection connection = new MySqlConnection(PcConnectionString))
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
                    catch (TimeoutException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _logger.Error(ex, "ReturnDataAsync > Error (TimeoutException ex)");
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
                                    reader.GetDateTime(1),
                                    reader.GetString(2)
                                ));
                            }
                            reader.Close();
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "ReturnDataAsync > Error (TimeoutException ex)");
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
            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
                    catch (TimeoutException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _logger.Error(ex, "GetIdlesAsync > Error (TimeoutException ex)");
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
                catch (TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "GetIdlesAsync > Error (TimeoutException ex)");
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

        public async Task InsertDataAsync()
        {
            bool isComplite = false;

            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString)) 
            {
                try
                {
                    await connection.OpenAsync();
                    var query = new System.Text.StringBuilder("INSERT INTO downtime (Timestamp, Difference, idIdle , Comment, Recept) VALUES ");

                    // Добавление всех значений в запрос
                    var valueList = new List<string>();
                    int countInt = 0;

                    foreach (var entry in datesNew)
                    {
                        if (entry._isPastData == false)
                        {
                            valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', '{entry.Difference}', '{entry.IdTypeDowntime}', '{entry.Comments.Replace("'", "''")}',  '{entry.Recept.Name}')");
                            countInt++;
                        }
                    }

                    // Соединяем все строки значений в один запрос
                    query.Append(string.Join(", ", valueList));
                    query.Append(";");

                    string queryUpdate = GetUpdateQueryAsync(datesPast);

                    if (countInt > 0 && queryUpdate != null)
                    {
                        var queryFull = query.ToString() + queryUpdate;
                        using (MySqlCommand cmd = new MySqlCommand(queryFull, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                            isComplite = true;
                        }
                    }
                    else if (countInt > 0 && queryUpdate == null)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(query.ToString(), connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                            isComplite = true;
                        }
                    }
                    else if (countInt <= 0 && queryUpdate != null)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(queryUpdate, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                            isComplite = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    if (isComplite)
                        MessageBox.Show("Данные сохранены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Ошибка сохранения");

                    ClearData();
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<List<Recept>> GetRecept()
        {
            string query = "SELECT Name, Time FROM spslogger.recepttime";

            List<Recept> recept = new List<Recept>();

            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
                catch (TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "GetRecept > Error (TimeoutException ex)");
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
            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
        public string GetUpdateQueryAsync(List<Date> datesNew)
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

        private List<Date> DeletesIdenticalData(ref List<Date> datesNew, List<Date> datesPast)
        {
            List<Date> result = new List<Date>();
            List<Date> delited = new List<Date>();
            foreach (var date in datesNew)
            {
                result.Add(date);
            }

            for(int i = 0; i < datesPast.Count; i++)
            {
                for(int j = 0; j < result.Count; j++)
                {
                    if (datesPast[i].Timestamp == result[j].Timestamp)
                    {
                        if (datesPast[i].Difference == result[j].Difference)
                        {
                            if (datesPast[i].Recept.Name == result[j].Recept.Name)
                            {
                                delited.Add(result[j]);
                                result[j] = datesPast[i];
                            }
                        }
                    }
                }
            }

            foreach (var item in delited)
            {
                datesNew.Remove(item);
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

        /// <summary>
        /// Очищает Все данные
        /// </summary>
        public void ClearData()
        {
            newDates.Clear();
            datesPast.Clear();
        }
        
        /// <summary>
        /// Возвращает общее время простоя
        /// </summary>
        /// <returns>TimeSpan</returns>
        public TimeSpan GetFullDowntime()
        {
            TimeSpan time = new TimeSpan();

            foreach (var item in _resultDate)
            {
                var valueTime = item.Recept.Time;
                time += valueTime;
            }

            return time;
        }

        private async Task<List<Recept>> GetLocalPCRecepts()
        {
            using (MySqlConnection connection = new MySqlConnection(PcConnectionString))
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
                            return null;
                        }
                    }
                    catch(TimeoutException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _logger.Error(ex, "GetLocalPCRecepts > Error (TimeoutException ex)");
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
                catch (TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "GetLocalPCRecepts > Error (TimeoutException ex)");
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
            _logger.Trace("GetServerRecepts > Start");
            string query = "SELECT Name FROM spslogger.receptTime group by Name;";

            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
                    _logger.Error(ex, "GetServerRecepts > Error (MySqlException ex)");

                }
                catch(TimeoutException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "GetServerRecepts > Error (TimeoutException ex)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _logger.Error(ex, "GetServerRecepts > Error (Exception ex)");
                }
                finally 
                { 
                    await connection.CloseAsync();
                    _logger.Trace("GetServerRecepts > END");
                }
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

        private async Task ChangeDBReceptTime(List<Recept> recepts)
        {
            string sqlInsert = "INSERT INTO spslogger.recepttime (Name, Time) VALUES (@Name, @Time)";
            
            using (MySqlConnection connection = new MySqlConnection(ServerConnectionString))
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
