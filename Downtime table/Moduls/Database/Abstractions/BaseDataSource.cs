using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Downtime_table.Moduls.Database
{
    internal abstract class BaseDataSource<T>
    {
        protected readonly string ConnectionString;
        protected readonly ILogger Logger;
        protected readonly List<RawDate> Dates = new List<RawDate>();
        protected const string ErrorOldBdMessage = "Unknown system variable 'lower_case_table_names'";

        /// <summary>
        /// Конструктор для реализации RawDataSource источника данных
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        /// <param name="logger">Логгер</param>
        protected BaseDataSource(string connectionString, ILogger logger)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString)); ;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
        }

        /// <summary>
        /// Получает данные из источника
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <returns>Список данных</returns>
        public abstract Task<List<T>> GetDataAsync(string query);

        /// <summary>
        /// Создает и открывает соединение с базой данных
        /// </summary>
        /// <returns>Открытое соединение с базой данных</returns>
        protected async Task<MySqlConnection> CreateConnectionAsync()
        {
            MySqlConnection connection = new MySqlConnection(ConnectionString);

            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch (MySqlException ex)
            {
                // Проверка на ошибку старой версии БД
                if (ex.Message.Contains(ErrorOldBdMessage))
                {
                    return connection;
                }
                Logger.Error(ex, $"Ошибка при открытии соединения с базой данных: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Неизвестная ошибка при создании соединения: {ex.Message}");
                throw;
            }
        }
    }
}
