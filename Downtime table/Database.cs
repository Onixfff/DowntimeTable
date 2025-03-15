using Downtime_table.Moduls;
using Downtime_table.Moduls.Data.Sources;
using Downtime_table.Moduls.Database.Sources;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Downtime_table
{
    public class Database
    {  
        private DataSet _dsIdle;
        
        private List<ProcessedDate> _processedDate = new List<ProcessedDate>();
        private List<ArchivedDate> _archivedDate = new List<ArchivedDate>();
        private List<RawDate> _rawDate = new List<RawDate>();
        private List<ArchivedDate> _resultDate = new List<ArchivedDate>();
        
        private List<string> _comments;
        private List<string> _recepts;
        private bool isNewData;
        List<DateIdle> _idles = new List<DateIdle>();
        private List<Recept> _LocalPCRecepts = new List<Recept>();
        private List<Recept> _ServerRecepts = new List<Recept>();
        private ILogger _logger;
        private ClassLibraryGetIp.Main _mainInstance = new ClassLibraryGetIp.Main();
        private string _pcConnectionString, _serverConnectionString;

        // Источники данных
        private RawDataSource _rawDataSource;
        private ArchivedDateSource _archivedDateSource;

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
                this._pcConnectionString = PcConnectionString.updateConnection;
                this._serverConnectionString = ServerConnectionString.updateConnection;
                
                // Инициализация источников данных после получения строк подключения
                _rawDataSource = new RawDataSource(this._pcConnectionString, _logger);
            }
        }

        public async Task<List<ViewDate>> GetMain(DateTime dateTime)
        {
            try
            {
                _logger.Trace("GetMain > Start");

                // Шаг 1: Получение и синхронизация рецептов
                await SynchronizeRecepts();

                // Шаг 2: Получение данных о простоях
                await LoadIdlesData();
                
                // Обновляем источник архивных данных с актуальным списком рецептов
                _archivedDateSource = new ArchivedDateSource(_serverConnectionString, _logger, _ServerRecepts);

                // Шаг 3: Формирование SQL-запросов на основе времени
                (string dataSql, string archiveSql) = GenerateSqlQueries(dateTime);

                // Шаг 4: Получение и обработка данных
                await ProcessData(dataSql, archiveSql);

            }
            catch (Exception ex) 
            {
                _logger.Error(ex, "Ошибка в GetMain");
                MessageBox.Show($"{ex.Message}");
            }

            return ChangeArchivedDateInViewDate();
        }

        // Метод для синхронизации рецептов
        private async Task SynchronizeRecepts()
        {
            _ServerRecepts = await GetServerRecepts();
            _LocalPCRecepts = await GetLocalPCRecepts();
            await ChecksDataDifferenceRecepts();
        }

        // Метод для загрузки данных о простоях
        private async Task LoadIdlesData()
        {
            _idles = await GetIdlesAsync();
        }

        // Метод для генерации SQL-запросов
        private (string dataSql, string archiveSql) GenerateSqlQueries(DateTime currentTime)
        {
            DateTime nextData = currentTime.AddDays(1);
            DateTime lastDate = currentTime.AddDays(-1);

            string sql, sqlLastData;

            if (currentTime.TimeOfDay >= new TimeSpan(8, 30, 0) && currentTime.TimeOfDay < new TimeSpan(20, 29, 0))
            {
                // Дневная смена
                sql = GenerateDataSql(currentTime, currentTime, "07:30:00", "20:29:00");
                sqlLastData = GenerateArchiveSql(currentTime, currentTime, "07:30:00", "20:29:00");
            }
            else if (currentTime.TimeOfDay <= new TimeSpan(24, 59, 59) && currentTime.TimeOfDay >= new TimeSpan(20, 00, 00))
            {
                // Вечерняя смена (начало)
                sql = GenerateDataSql(currentTime, nextData, "20:00:00", "08:00:00");
                sqlLastData = GenerateArchiveSql(currentTime, nextData, "20:00:00", "08:00:00");
            }
            else if (currentTime.TimeOfDay <= new TimeSpan(8, 29, 00))
            {
                // Вечерняя смена (конец)
                sql = GenerateDataSql(lastDate, currentTime, "20:00:00", "08:00:00");
                sqlLastData = GenerateArchiveSql(lastDate, currentTime, "20:00:00", "08:00:00");
            }
            else
            {
                throw new Exception("Ошибка времени");
            }

            return (sql, sqlLastData);
        }

        // Вспомогательные методы для генерации SQL
        private string GenerateDataSql(DateTime startDate, DateTime endDate, string startTime, string endTime)
        {
            return $"SELECT DBID, Timestamp, Data_52 FROM spslogger.mixreport " +
                   $"WHERE Timestamp >= '{startDate:yyyy-MM-dd} {startTime}' " +
                   $"AND Timestamp < '{endDate:yyyy-MM-dd} {endTime}'";
        }

        private string GenerateArchiveSql(DateTime startDate, DateTime endDate, string startTime, string endTime)
        {
            return $"SELECT f1.Id, f1.Timestamp, f1.Difference, f2.Name, f2.Time, f1.idIdle, f3.name, f1.Comment " +
                   $"FROM downTime AS f1 " +
                   $"LEFT JOIN recepttime AS f2 ON f1.Recept = f2.Name " +
                   $"LEFT JOIN ididles AS f3 ON f1.idIdle = f3.name " +
                   $"WHERE Timestamp >= '{startDate:yyyy-MM-dd} {startTime}' " +
                   $"AND Timestamp < '{endDate:yyyy-MM-dd} {endTime}'";
        }

        // Метод для обработки данных
        private async Task ProcessData(string dataSql, string archiveSql)
        {
            // Используем источники данных вместо прямых запросов
            _rawDate = await _rawDataSource.GetDataAsync(dataSql);

            // Обновляем список рецептов в источнике архивных данных перед запросом
            _archivedDateSource = new ArchivedDateSource(_serverConnectionString, _logger, _ServerRecepts);
            _archivedDate = await _archivedDateSource.GetDataAsync(archiveSql);

            _processedDate = CalculateDowntime(_rawDate);
            _resultDate = DeletesIdenticalData(_processedDate, _archivedDate);
        }

        /// <summary>
        /// Возвращает преборазованные данные в view формате для фронта
        /// </summary>
        /// <returns>Список данных в формате ViewDate</returns>
        private List<ViewDate> ChangeArchivedDateInViewDate()
        {
            List<ViewDate> viewDates = new List<ViewDate>();

            if (_resultDate != null && _resultDate.Count > 0)
            {
                // Преобразуем каждый элемент списка ArchivedDate в ViewDate
                foreach (var date in _resultDate)
                {
                    viewDates.Add(new ViewDate(date));
                }
            }
            return viewDates;
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

        //private List<Date> ChangeViewResult(Recept recept, List<Date> main)
        //{
        //    List<Date> result = new List<Date>();

        //    foreach (var item in result)
        //    {
        //        if(item.Recept == recept)
        //        {
        //            result.Add(item);
        //        }
        //    }

        //    return result;
        //}

        private List<ProcessedDate> CalculateDowntime(List<RawDate> rawData)
        {
            //Четные числа. 0,2,4,6,8,10 ... . Открывающие числа
            //Нечетные числа 1,3,5,7,9 ... закрывающие числа

            List<ProcessedDate> datesNew = new List<ProcessedDate>();

            // Проверяем, что список не пустой и начинается с четного числа
            if (rawData == null || rawData.Count == 0)
            {
                return datesNew;
            }

            // Проверяем, что первая запись четная
            if (rawData[0].DBIG % 2 != 0)
            {
                throw new Exception("Данные должны начинаться с четного числа (открытие)");
            }

            for (int i = 0; i < rawData.Count - 1; i += 2) // Переходим по две записи
            {
                var openRecord = rawData[i];
                var closeRecord = rawData[i + 1];

                // Проверяем, что текущая запись четная (открытие)
                if (openRecord.DBIG % 2 != 0)
                {
                    _logger.Error($"Ожидалось четное число (открытие), получено: {openRecord.DBIG}");
                    throw new Exception($"Ожидалось четное число (открытие), получено: {openRecord.DBIG}");
                }

                // Проверяем, что следующая запись нечетная (закрытие)
                if (closeRecord.DBIG % 2 != 1)
                {
                    _logger.Error($"Ожидалось нечетное число (закрытие), получено: {closeRecord.DBIG}");
                    throw new Exception($"Ожидалось нечетное число (закрытие), получено: {closeRecord.DBIG}");
                }

                // Проверяем, что записи относятся к одному рецепту
                if (openRecord.NameRecept != closeRecord.NameRecept)
                {
                    _logger.Error($"Несоответствие рецептов: {openRecord.NameRecept} != {closeRecord.NameRecept}");
                    throw new Exception($"Несоответствие рецептов: {openRecord.NameRecept} != {closeRecord.NameRecept}");
                }

                Recept recept = _ServerRecepts?.FirstOrDefault(r => r.Name == openRecord.NameRecept);

                if (recept == null)
                {
                    _logger.Error($"Рецепт не найден: {openRecord.NameRecept}");
                    throw new Exception($"Рецепт не найден: {openRecord.NameRecept}");
                }

                // Вычисляем разницу времени
                TimeSpan result = closeRecord.DateTime - openRecord.DateTime;
                Console.WriteLine($"Начало {openRecord.DateTime} - {openRecord.NameRecept} сравнивается с {closeRecord.DateTime} - {closeRecord.NameRecept}");

                // Проверяем, превышает ли разница ожидаемое время
                if (result.TotalSeconds >= recept.Time.TotalSeconds)
                {
                    datesNew.Add(new ProcessedDate(openRecord.DBIG, openRecord.DateTime, result - recept.Time, recept));
                }
            }

            return datesNew;
        }

        public async Task<List<DateIdle>> GetIdlesAsync()
        {
            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString))
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
            for (int i = 0; i < _processedDate.Count; i++)
            {
                if (_processedDate[i].Id == id)
                {
                    _processedDate[i].IdTypeDowntime = idIdle;
                }
            }

            for(int i = 0; i < _archivedDate.Count; i++)
            {
                if (_archivedDate[i].Id == id)
                {
                    _archivedDate[i].IdTypeDowntime = idIdle;
                }
            }
        }

        public void ChangeData(int id, string comment)
        {

            for (int i = 0; i < _processedDate.Count; i++)
            {
                if (_processedDate[i].Id == id)
                {
                    _processedDate[i].Comment = comment;
                }
            }

            for (int i = 0; i < _archivedDate.Count; i++)
            {
                if (_archivedDate[i].Id == id)
                {
                    _archivedDate[i].Comment = comment;
                }
            }
        }

        public async Task InsertDataAsync()
        {
            bool isComplite = false;

            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString)) 
            {
                try
                {
                    await connection.OpenAsync();
                    var query = new System.Text.StringBuilder("INSERT INTO downtime (Timestamp, Difference, idIdle , Comment, Recept) VALUES ");

                    // Добавление всех значений в запрос
                    var valueList = new List<string>();
                    int countInt = 0;

                    foreach (var entry in _processedDate)
                    {
                        if (entry.IsPastData == false)
                        {
                            valueList.Add($"('{entry.Timestamp:yyyy-MM-dd HH:mm:ss}', '{entry.Difference}', '{entry.IdTypeDowntime}', '{entry.Comment.Replace("'", "''")}',  '{entry.Recept.Name}')");
                            countInt++;
                        }
                    }

                    // Соединяем все строки значений в один запрос
                    query.Append(string.Join(", ", valueList));
                    query.Append(";");

                    string queryUpdate = GetUpdateQueryAsync(_archivedDate);

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

            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString))
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
            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString))
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

        public List<ViewDate> GetListDate()
        {
            return ChangeArchivedDateInViewDate();
        }

        /// <summary>
        /// Возвращает string sql для обновления
        /// </summary>
        /// <param name="datesNew">Данные для обновления</param>
        /// <returns>if(datesNew.Count <= 0) return null; else return string sql</returns>
        public string GetUpdateQueryAsync(List<ArchivedDate> datesNew)
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
                query += $" WHEN Id = {datesNew[i].Id} THEN '{datesNew[i].Comment}'";
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

        /// <summary>
        /// Объединяет новые и архивные данные, удаляя дубликаты
        /// </summary>
        /// <param name="newData">Список новых обработанных данных</param>
        /// <param name="archivedData">Список архивных данных</param>
        /// <returns>Объединенный список данных без дубликатов</returns>
        private List<ArchivedDate> DeletesIdenticalData(List<ProcessedDate> newData, List<ArchivedDate> archivedData)
        {
            // Создаем результирующий список
            List<ArchivedDate> result = new List<ArchivedDate>();

            // Добавляем все архивные данные в результат
            foreach (var archived in archivedData)
            {
                result.Add(archived); // Архивные данные уже имеют IsPastData = true
            }

            // Создаем хеш-сет для быстрого поиска дубликатов
            HashSet<string> existingKeys = new HashSet<string>();

            // Добавляем ключи существующих элементов
            foreach (var item in result)
            {
                string key = $"{item.Timestamp}_{item.Difference}_{item.Recept.Name}";
                existingKeys.Add(key);
            }

            // Проходим по новым данным
            foreach (var newItem in newData)
            {
                // Создаем ключ для текущего элемента
                string key = $"{newItem.Timestamp}_{newItem.Difference}_{newItem.Recept.Name}";

                // Проверяем, есть ли такой элемент уже в результате
                if (!existingKeys.Contains(key))
                {
                    // Создаем новый ArchivedDate на основе ProcessedDate
                    ArchivedDate newArchivedItem = new ArchivedDate(
                        newItem.Id,
                        newItem.Timestamp,
                        newItem.Difference,
                        newItem.Recept,
                        newItem.IdTypeDowntime,
                        newItem.TypeDownTime,
                        newItem.Comment,
                        false // Новые данные имеют IsPastData = false
                    );

                    result.Add(newArchivedItem);
                    existingKeys.Add(key); // Добавляем ключ в хеш-сет
                }
            }

            return result;
        }

        public bool ChecksFieldsAreFilledIn()
        {
            bool checkDatesNew = true, checkDatesPast = true;

            for (int i = 0; i < _processedDate.Count; i++)
            {
                if (_processedDate[i].Comment != null)
                {
                    if (_processedDate[i].IdTypeDowntime >= 0)
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

            for (int i = 0; i < _archivedDate.Count; i++)
            {
                if (_archivedDate[i].Comment != null)
                {
                    if (_archivedDate[i].IdTypeDowntime >= 0)
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
            _processedDate.Clear();
            _rawDate.Clear();
            _archivedDate.Clear();
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
            using (MySqlConnection connection = new MySqlConnection(_pcConnectionString))
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

            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString))
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

        private async Task ChecksDataDifferenceRecepts()
        {
            if (_LocalPCRecepts != null && _ServerRecepts != null)
            {
                List<Recept> recepts = _LocalPCRecepts.Where(r1 => !_ServerRecepts.Any(r2 => r2.Name == r1.Name)).ToList();

                if (recepts.Count >= 1)
                {
                    await ChangeDBReceptTime(recepts);
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
            
            using (MySqlConnection connection = new MySqlConnection(_serverConnectionString))
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
