﻿using Dapper;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Logging;


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
            logger.Debug(GetType().Name, dropTableQuery);

            _rwl.TryEnterWriteLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                connection.Execute(dropTableQuery);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        internal void ResetTable(ILogger logger, Type type)
        {
            DropTable(logger, type);

            CreateTable(logger, type);
        }

        internal void InsertValue<T>(ILogger logger, T obj)
        {
            string insertValueQuery = SqliteQueryHelper.GetInsertQuery(obj);

            InsertValue(logger, insertValueQuery);
        }

        internal void InsertValue(ILogger logger, string insertValueQuery)
        {
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
            logger.Debug(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int recordCount = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Debug(GetType().Name, $"Total count {recordCount}");
                return recordCount;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal int GetMaxValue(ILogger logger, Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MAX({columnName}) FROM {type.Name};";
            logger.Debug(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int maxValue = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Debug(GetType().Name, $"Max Value {maxValue}");
                return maxValue;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal int GetMinValue(ILogger logger, Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MIN({columnName}) FROM {type.Name};";
            logger.Debug(GetType().Name, countRecordsQuery);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                int minValue = connection.ExecuteScalar<int>(countRecordsQuery);

                logger.Debug(GetType().Name, $"Min Value {minValue}");
                return minValue;
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal IEnumerable<T> GetRecords<T>(ILogger logger, string query)
        {
            logger.Debug(GetType().Name, query);

            _rwl.TryEnterReadLock(rwlMillisecondsTimeout);
            try
            {
                using Microsoft.Data.Sqlite.SqliteConnection connection = new(_connectionString);

                connection.Open();

                return connection.Query<T>(query);
            }
            finally { _rwl.ExitReadLock(); }
        }

        internal IEnumerable<T> GetAllRecords<T>(ILogger logger)
        {
            int batchCount = 0;
            int batchSize = 5000;

            IEnumerable<T> collRecords;
            do
            {
                int offset = batchSize * batchCount;
                string query = @$"
                    SELECT * 
                    FROM {typeof(T).Name} 
                    LIMIT {batchSize} OFFSET {offset};";

                collRecords = GetRecords<T>(logger, query);

                foreach (var record in collRecords)
                {
                    yield return record;
                }

                batchCount++;

            } while (collRecords.Any());
        }

    }
}
