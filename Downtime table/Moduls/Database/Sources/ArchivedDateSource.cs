using Downtime_table.Moduls.Data.Sources;
using Downtime_table.Moduls.Database.Abstractions;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Downtime_table.Moduls.Database.Sources
{
    internal class ArchivedDateSource : ReceptDataSource<ArchivedDate>
    {
        public ArchivedDateSource(string connectionString, ILogger logger, List<Recept> recepts) : base(connectionString, logger, recepts){}

        protected override async Task<List<ArchivedDate>> GetDataWithReceptsAsync(string query, List<Recept> recepts)
        {
            List<ArchivedDate> datesLocal = new List<ArchivedDate>();

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
                                if (Recepts != null && Recepts.Count > 0)
                                {
                                    var name = reader.IsDBNull(3) ? null : reader.GetString(3);
                                    var matchingRecept = Recepts.FirstOrDefault(item => item.Name == name);
                                    if (matchingRecept != null)
                                    {
                                        string StringTypeDownTime = !await reader.IsDBNullAsync(6) ? reader.GetString(6) : "Нету данных";
                                        string Comment = !await reader.IsDBNullAsync(7) ? reader.GetString(7) : "Нету данных";

                                        datesLocal.Add(new ArchivedDate(
                                            reader.GetInt32(0),
                                            reader.GetDateTime(1),
                                            reader.GetTimeSpan(2),
                                            matchingRecept,
                                            reader.GetInt32(5),
                                            StringTypeDownTime,
                                            Comment,
                                            true
                                        ));
                                    }
                                }
                            }
                            reader.Close();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Logger.Error(ex, "ReturnLastDataAsync > Error MySqlException");
                    throw;
                }
                catch (TimeoutException ex)
                {
                    Logger.Error(ex, "ReturnLastDataAsync > Error TimeoutException");
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "ReturnLastDataAsync > Error Exception");
                    throw;
                }
            }
            return datesLocal;
        }
    }
}
