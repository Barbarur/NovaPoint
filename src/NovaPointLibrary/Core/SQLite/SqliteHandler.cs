using Dapper;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Logging;
using System.IO;


namespace NovaPointLibrary.Core.SQLite
{
    internal class SqliteHandler
    {

        private static readonly SqliteHandler _cacheHandler = new(CacheConnectionString);

        internal static string CacheConnectionString
        {
            get
            {
                string bdPath = Path.Combine(AppSettings.GetLocalAppPath(), "solutioncache.db");
                return $"Data Source={bdPath};";
            }
        }

        private readonly string _connectionString;
        private readonly ReaderWriterLockSlim _rwl = new();
        private readonly int rwlMillisecondsTimeout = 3000;

        private SqliteHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        internal static SqliteHandler GetCacheHandler()
        {
            return _cacheHandler;
        }

        internal void CreateTable(ILogger logger, string createTableQuery)
        {
            _rwl.TryEnterWriteLock(rwlMillisecondsTimeout);
            try
            {
                logger.Debug(GetType().Name, createTableQuery);

                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                connection.Execute(createTableQuery);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        internal void CreateTable(ILogger logger, Type type)
        {
            string createTableQuery = SqliteQueryHelper.GetCreateTableQuery(type);

            CreateTable(logger, createTableQuery);
        }

        internal void DropTable(ILogger logger, Type type)
        {
            string dropTableQuery = $"DROP TABLE IF EXISTS {type.Name};";
            logger.Info(GetType().Name, dropTableQuery);

            _rwl.TryEnterWriteLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                connection.Execute(dropTableQuery);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        internal void ResetTableQuery(ILogger logger, Type type)
        {
            DropTable(logger, type);

            CreateTable(logger, type);
        }

        internal void InsertValue<T>(ILogger logger, T obj)
        {
            string insertValueQuery = SqliteQueryHelper.GetInsertQuery(obj);
            logger.Debug(GetType().Name, insertValueQuery);

            _rwl.TryEnterWriteLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                connection.Execute(insertValueQuery);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        internal int GetCountTotalRecord(ILogger logger, Type type)
        {
            string countRecordsQuery = $"SELECT COUNT(*) FROM {type.Name};";
            logger.Info(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int recordCount = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Info(GetType().Name, $"Total count {recordCount}");
                return recordCount;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal int GetMaxValue(ILogger logger, Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MAX({columnName}) FROM {type.Name};";
            logger.Info(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int maxValue = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Info(GetType().Name, $"Max Value {maxValue}");
                return maxValue;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal int GetMinValue(ILogger logger, Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MIN({columnName}) FROM {type.Name};";
            logger.Info(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int minValue = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Info(GetType().Name, $"Min Value {minValue}");
                return minValue;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal IEnumerable<T> GetRecords<T>(ILogger logger, string query)
        {
            logger.Info(GetType().Name, query);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                return connection.Query<T>(query);
            }
            finally { _rwl.ExitReadLock(); }
        }

    }
}
