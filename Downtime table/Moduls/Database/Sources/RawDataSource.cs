using MySql.Data.MySqlClient;
using NLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Windows.Forms;
using Downtime_table.Moduls.Database;

namespace Downtime_table.Moduls
{
    internal class RawDataSource : BaseDataSource<RawDate>
    {

        public RawDataSource(string connectionString, ILogger logger) : base(connectionString, logger)
        {
        }

        public override async Task<List<RawDate>> GetDataAsync(string query)
        {
            List<RawDate> newDates = new List<RawDate>();

            // Логика получения необработанных данных
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
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
                        if (ex.Message == ErrorOldBdMessage)
                            goto Select;
                        else
                        {
                            Logger.Error(ex, "RawDataSource > Error MySqlException");
                            await connection.CloseAsync();
                            throw;
                        }

                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Error(ex, "RawDataSource > Error TimeoutException");
                        await connection.CloseAsync();
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "RawDataSource > Неизвестная ошибка");
                        await connection.CloseAsync();
                        throw;
                    }
                Select:

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
                }
                catch (TimeoutException ex)
                {
                    Logger.Error(ex, "RawDataSource > Error TimeoutException");
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

            return newDates;
        }
    }
}
