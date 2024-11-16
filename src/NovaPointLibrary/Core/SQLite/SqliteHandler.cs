using Dapper;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Core.SQLite
{
    internal class SqliteHandler
    {
        private readonly ILogger _logger;
        internal static string ConnectionString
        {
            get
            {
                string bdPath = Path.Combine(AppSettings.GetLocalAppPath(), "solutioncache.db");
                return $"Data Source={bdPath};";
            }
        }
        internal SqliteHandler(ILogger logger)
        {
            _logger = logger;
        }

        internal void CreateTable(string createTableQuery)
        {
            _logger.Debug(GetType().Name, createTableQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            connection.Execute(createTableQuery);
        }

        internal void CreateTable(Type type)
        {
            string createTableQuery = SqliteQueryHelper.GetCreateTableQuery(type);

            CreateTable(createTableQuery);
        }

        internal void DropTable(Type type)
        {
            string dropTableQuery = $"DROP TABLE IF EXISTS {type.Name};";
            _logger.Info(GetType().Name, dropTableQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            connection.Execute(dropTableQuery);
        }

        internal void ResetTableQuery(Type type)
        {
            DropTable(type);

            CreateTable(type);
        }

        internal void InsertValue<T>(T obj)
        {
            string insertValueQuery = SqliteQueryHelper.GetInsertQuery(obj);
            _logger.Debug(GetType().Name, insertValueQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            connection.Execute(insertValueQuery);
        }

        internal int GetCountTotalRecord(Type type)
        {
            string countRecordsQuery = $"SELECT COUNT(*) FROM {type.Name};";
            _logger.Info(GetType().Name, countRecordsQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            int recordCount = connection.ExecuteScalar<int>(countRecordsQuery);

            _logger.Info(GetType().Name, $"Total count {recordCount}");
            return recordCount;
        }

        internal int GetMaxValue(Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MAX({columnName}) FROM {type.Name};";
            _logger.Info(GetType().Name, countRecordsQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            int maxValue = connection.ExecuteScalar<int>(countRecordsQuery);

            _logger.Info(GetType().Name, $"Max Value {maxValue}");
            return maxValue;
        }

        internal int GetMinValue(Type type, string columnName)
        {
            string countRecordsQuery = $"SELECT MIN({columnName}) FROM {type.Name};";
            _logger.Info(GetType().Name, countRecordsQuery);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            int minValue = connection.ExecuteScalar<int>(countRecordsQuery);

            _logger.Info(GetType().Name, $"Min Value {minValue}");
            return minValue;
        }

        internal IEnumerable<T> GetRecords<T>(string query)
        {
            _logger.Info(GetType().Name, query);

            using Microsoft.Data.Sqlite.SqliteConnection connection = new(ConnectionString);

            connection.Open();

            return connection.Query<T>(query);
        }

    }
}
