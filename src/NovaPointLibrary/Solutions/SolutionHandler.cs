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
        
        private readonly CancellationTokenSource _cancelTokenSource = new();
        
        public string SolutionFolder { get; set; } = NovaPointLibrary.Core.Settings.AppFolders.GetOutputFolder();

        // Guards the UI fields against concurrent UILog calls from parallel workers.
        // Notify with Post, never Send: a synchronous marshal from in here deadlocks.
        private readonly object _uiLogLock = new();

        // The context that constructed this handler; UI hosts require notifications on it.
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

        // This handler runs one solution, once. Construct a new one for every run, so
        // parallel runs are independent instances sharing no state.
        private const int NotStarted = 0, Running = 1, Finished = 2;
        private int _runState = NotStarted;

        public bool IsRunning => Volatile.Read(ref _runState) == Running;

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

        private string _uiText = string.Empty;
        public string UiText
        {
            get { return _uiText; }
            set
            {
                _uiText = value;
                OnPropertyChanged();
            }
        }



        public Task RunSolution()
        {
            if (Interlocked.CompareExchange(ref _runState, Running, NotStarted) != NotStarted)
            {
                throw new InvalidOperationException($"{_solutionName} has already been run by this handler. Create a new SolutionHandler for every run.");
            }
            OnPropertyChanged(nameof(IsRunning));

            LoggerSolution logger = new(UILog, _solutionName, param);
            SolutionFolder = logger._solutionFolderPath;

            return Task.Run(async () =>
            {
                try
                {
                    ContextSolution ctx = GetContext(logger);

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
                }
                finally
                {
                    Volatile.Write(ref _runState, Finished);
                    OnPropertyChanged(nameof(IsRunning));
                }
            });
        }

        public void Cancel()
        {
            _cancelTokenSource.Cancel();
        }

        internal IAppClient GetAppClient(LoggerSolution logger)
        {
            if (appProperties is AppClientConfidentialProperties confidentialProperties)
            {
                return new AppClientConfidential(confidentialProperties, logger, _cancelTokenSource);
            }
            else if (appProperties is AppClientPublicProperties publicProperties)
            {
                return new AppClientPublic(publicProperties, logger, _cancelTokenSource);
            }
            else
            {
                throw new Exception("App properties is neither public or confidential. Please check your settings.");
            }
        }

        private ContextSolution GetContext(LoggerSolution logger)
        {
            try
            {
                var appClient = GetAppClient(logger);

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
            lock (_uiLogLock)
            {
                if (!string.IsNullOrWhiteSpace(logInfo.TextBase)) { UiText += $"{logInfo.TextBase} \n"; }

                if (!string.IsNullOrWhiteSpace(logInfo.TextError)) { UiText += $"ERROR: {logInfo.TextError} \n"; }

                if (logInfo.PercentageProgress != -1)
                {
                    SetPendingTime(logInfo.PendingTime);
                    Progress = logInfo.PercentageProgress;
                }
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
            OnUiContext(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        private void OnUiContext(Action action)
        {
            if (SynchronizationContext.Current == _uiContext) { action(); }
            else { _uiContext.Post(_ => action(), null); }
        }
    }

   
}
