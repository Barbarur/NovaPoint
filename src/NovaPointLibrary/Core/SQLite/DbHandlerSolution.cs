using NovaPointLibrary.Core.Logging;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace NovaPointLibrary.Core.SQLite
{
    internal class DbHandlerSolution
    {
        private readonly LoggerSolution _logger;
        private Dictionary<Type, string> _solutionReports = new();
        private readonly SqliteHandler _sql = SqliteHandler.GetCacheHandler();

        internal DbHandlerSolution(LoggerSolution logger)
        {
            _logger = logger;
        }
        internal void AddSolutionReports(Dictionary<Type, string> dicSolutions)
        {
            _solutionReports = dicSolutions;

            ResetCache();
        }

        internal void End()
        {
            ExportAllReports();

            ClearCache();
        }

        private void ResetCache()
        {
            foreach (var key in _solutionReports.Keys)
            {
                _sql.ResetTable(_logger, key);
            }
        }

        public void WriteRecord<T>(T record)
        {
            _sql.InsertValue(_logger, record);
        }

        internal void WriteToCsv<ISolutionRecord>(IEnumerable<ISolutionRecord> records, string reportName)
        {
            _logger.Info(GetType().Name, $"Exporting {reportName}");
            
            string reportPath = Path.Combine(_logger._solutionFolderPath, _logger._solutionFileName + $"_{reportName}.csv");
            PropertyInfo[] properties = typeof(ISolutionRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using StreamWriter csv = new(new FileStream(reportPath, FileMode.Append, FileAccess.Write));
            WriteCsvHeader(csv, properties);
            foreach (var record in records)
                WriteCsvRow(csv, properties, record);
        }

        private static void WriteCsvHeader(StreamWriter csv, PropertyInfo[] properties)
        {
            var sb = new StringBuilder();
            foreach (var p in properties)
                sb.Append($"\"{p.Name}\",");
            if (sb.Length > 0) sb.Length--;
            csv.WriteLine(sb.ToString());
        }

        private static void WriteCsvRow(StreamWriter csv, PropertyInfo[] properties, object? record)
        {
            var sb = new StringBuilder();
            foreach (var p in properties)
            {
                string s = $"{p.GetValue(record)}";
                sb.Append($"\"{s.Replace("\"", "\"\"")}\",");
            }
            if (sb.Length > 0) sb.Length--;
            csv.WriteLine(Regex.Replace(sb.ToString(), @"\r\n?|\n", ""));
        }

        private void ClearCache()
        {
            foreach (var key in _solutionReports.Keys)
            {
                _sql.DropTable(_logger, key);
            }
        }

        private void ExportAllReports()
        {
            if (_solutionReports == null) { return; }

            _logger.Info(GetType().Name, "Exporting all reports");

            foreach (var entry in _solutionReports)
            {
                var type = entry.Key;
                var reportName = entry.Value;

                var method = this.GetType().GetMethod(nameof(ExportReportToCsv), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    throw new InvalidOperationException($"Method to export to CSV not found on {this.GetType().Name}.");
                }
                var genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(this, new object[] { reportName });
            }
        }

        private void ExportReportToCsv<ISolutionRecord>(string reportName)
        {
            _logger.Info(GetType().Name, $"Exporting {reportName}");

            string reportPath = Path.Combine(_logger._solutionFolderPath, _logger._solutionFileName + $"_{reportName}.csv");
            PropertyInfo[] properties = typeof(ISolutionRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using StreamWriter csv = new(new FileStream(reportPath, FileMode.Append, FileAccess.Write));
            WriteCsvHeader(csv, properties);
            foreach (var record in _sql.GetAllRecords<ISolutionRecord>(_logger))
                WriteCsvRow(csv, properties, record);
        }
    }
}
