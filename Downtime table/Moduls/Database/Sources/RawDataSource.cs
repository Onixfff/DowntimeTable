using MySql.Data.MySqlClient;
using NLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Downtime_table.Moduls.Database;

namespace Downtime_table.Moduls
{
    internal class RawDataSource : BaseDataSource<RawDate>
    {
        public RawDataSource(string connectionString, ILogger logger) : base(connectionString, logger) {}

        public override async Task<List<RawDate>> GetDataAsync(string query)
        {
            List<RawDate> newDates = new List<RawDate>();

            // Логика получения необработанных данных
            using (MySqlConnection connection = await CreateConnectionAsync())
            {
                try
                {
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                newDates.Add(new RawDate
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
                catch (MySqlException ex)
                {
                    Logger.Error(ex, "RawDataSource > Error MysqlException");
                    throw;
                }
                catch (TimeoutException ex)
                {
                    Logger.Error(ex, "RawDataSource > Error TimeoutException");
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "RawDataSource > Error Exception");
                    throw;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return newDates;
        }
    }
}
