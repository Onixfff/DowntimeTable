using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Downtime_table.Moduls.Database.Abstractions
{
    internal abstract class ReceptDataSource<T> : BaseDataSource<T>
    {
        protected readonly List<Recept> Recepts;

        protected ReceptDataSource(string connectionString, ILogger logger, List<Recept> recepts) : base(connectionString, logger)
        {
            Recepts = recepts ?? throw new ArgumentNullException(nameof(recepts));
        }

        /// <summary>
        /// Получает данные из источника с использованием списка рецептов
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <param name="recepts">Список рецептов (если отличается от установленного в конструкторе)</param>
        /// <returns>Список данных</returns>
        public override Task<List<T>> GetDataAsync(string query)
        {
            return GetDataWithReceptsAsync(query, Recepts);
        }

        /// <summary>
        /// Получает данные из источника с использованием списка рецептов
        /// </summary>
        /// <param name="query">SQL-запрос</param>
        /// <param name="recepts">Список рецептов</param>
        /// <returns>Список данных</returns>
        protected abstract Task<List<T>> GetDataWithReceptsAsync(string query, List<Recept> recepts);
    }
}
