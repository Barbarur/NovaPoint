using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace NovaPointLibrary.Solutions
{
    public class SolutionHandler(Func<ContextSolution, ISolutionParameters, ISolution> solutionCreate, ISolutionParameters param, IAppClientProperties appProperties) : INotifyPropertyChanged
    {

        private readonly string _solutionName = solutionCreate.Method.DeclaringType != null ? solutionCreate.Method.DeclaringType.Name : "unknown";
        
        public CancellationTokenSource CancelTokenSource { get; set; } = new();
        
        public string SolutionFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static readonly ReaderWriterLock rwl = new();

        private string _percentageCompleted = "0%";
        public string PercentageCompleted
        {
            get { return _percentageCompleted; }
            set
            {
                _percentageCompleted = $"{value}%";
                OnPropertyChanged();
            }
        }

        private double _progress = 0;
        public double Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                PercentageCompleted = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _pendingTime = "Calculating time to complete";
        public string PendingTime
        {
            get { return _pendingTime; }
            set
            {
                _pendingTime = value;
                OnPropertyChanged();
            }
        }

        public Task RunSolution()
        {
            LoggerSolution logger = new(UILog, _solutionName, param);
            SolutionFolder = logger._solutionFolderPath;

            return Task.Run(async () =>
            {
                ContextSolution ctx = await GetContext(logger);

                try
                {
                    var oSolution = solutionCreate(ctx, param);

                    await oSolution.RunAsync();

                    ctx.SolutionEnd();
                }
                catch (Exception ex)
                {
                    ctx.SolutionEnd(ex);
                }
            });
        }

        internal IAppClient GetAppClient(LoggerSolution logger)
        {
            CancelTokenSource = new();

            if (appProperties is AppClientConfidentialProperties confidentialProperties)
            {
                return new AppClientConfidential(confidentialProperties, logger, CancelTokenSource);
            }
            else if (appProperties is AppClientPublicProperties publicProperties)
            {
                return new AppClientPublic(publicProperties, logger, CancelTokenSource);
            }
            else
            {
                throw new Exception("App properties is neither public or confidential. Please check your settings.");
            }
        }

        private async Task<ContextSolution> GetContext(LoggerSolution logger)
        {
            try
            {
                var appClient = GetAppClient(logger);

                string url = $"/sites/root";
                var graphSiteRoot = await new GraphAPIHandler(logger, appClient).GetObjectAsync<GraphSitesRoot>(url);
                logger.Info("AppClient", $"Hostname: {graphSiteRoot.SiteCollection.Hostname}");

                string domain = graphSiteRoot.SiteCollection.Hostname.Remove(graphSiteRoot.SiteCollection.Hostname.IndexOf(".sharepoint.com", StringComparison.OrdinalIgnoreCase));
                logger.Info("AppClient", $"Domain: {domain}");

                appClient.Domain = domain;

                return new(logger, appClient, new(logger));
            }

            catch (Exception ex)
            {
                logger.End(ex);
                throw;
            }
        }



        public void UILog(LogInfo logInfo)
        {
            // Reference: https://stackoverflow.com/questions/2382663/ensuring-that-things-run-on-the-ui-thread-in-wpf
            rwl.AcquireWriterLock(3000);
            try
            {
                //if (!string.IsNullOrEmpty(logInfo.SolutionFolder))
                //{
                //    SolutionFolder = logInfo.SolutionFolder;
                //}

                //if (!string.IsNullOrWhiteSpace(logInfo.TextBase)) { UiTxtLogs.Add(logInfo); }

                //if (!string.IsNullOrWhiteSpace(logInfo.TextError)) { UiTxtLogs.Add(logInfo); }

                //// TEST
                //if (!string.IsNullOrWhiteSpace(logInfo.TextError)) { Notification += logInfo.TextError; }

                if (logInfo.PercentageProgress != -1)
                {
                    SetPendingTime(logInfo.PendingTime);
                    Progress = logInfo.PercentageProgress;
                }
            }

            finally
            {
                rwl.ReleaseLock();
            }
        }

        private string SetPendingTime(TimeSpan pendingTimeSpan)
        {
            string stringPendingTime = $"Pending Time: ";

            if (pendingTimeSpan.Days > 0)
            {
                stringPendingTime += $"{pendingTimeSpan.Days}d:";
            }
            stringPendingTime += $"{pendingTimeSpan.Hours}h:{pendingTimeSpan.Minutes}m:{pendingTimeSpan.Seconds}s";

            PendingTime = stringPendingTime;

            return stringPendingTime;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
