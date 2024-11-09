using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace NovaPointWPF.Pages.Solutions
{
    /// <summary>
    /// Interaction logic for SolutionBasePage.xaml
    /// </summary>
    public partial class SolutionBasePage : Page
    {
        static ReaderWriterLock rwl = new ReaderWriterLock();
        private readonly ISolutionForm _solutionForm;

        private string _solutionFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string SolutionFolder
        {
            get
            {
                return _solutionFolder;
            }
            set
            {
                if (Directory.Exists(value))
                {
                    FilesButton.IsEnabled = true;
                    _solutionFolder = value;
                };
            }
        }

        private TimeSpan _pendingTimeSpan = TimeSpan.Zero;
        private TimeSpan PendingTimeSpan
        {
            get
            {
                return _pendingTimeSpan;
            }
            set
            {
                _pendingTimeSpan = value;

                string pendingTime = $"Pending Time: ";

                if (_pendingTimeSpan.Days > 0)
                {
                    pendingTime += $"{_pendingTimeSpan.Days}d:";
                }
                pendingTime += $"{_pendingTimeSpan.Hours}h:{_pendingTimeSpan.Minutes}m:{_pendingTimeSpan.Seconds}s";

                if (PendingTime.Dispatcher.CheckAccess())
                {
                    PendingTime.Text = pendingTime;
                }
                else
                {
                    PendingTime.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        PendingTime.Text = pendingTime;
                    }));
                }
            }
        }

        public CancellationTokenSource CancelTokenSource { get; set; } = new();

        public SolutionBasePage(ISolutionForm solutionForm)
        {
            InitializeComponent();

            DataContext = this;

            SolutionFormFrame.Content = solutionForm;

            _solutionForm = solutionForm;

            ResetProgress();
        }

        private void ResetProgress()
        {
            Progress.Value = 0;
            PercentageCompleted.Text = "Percentage completed";
            PendingTime.Text = "Pending time to complete";
            BoxText.Text = String.Empty;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            //Frame? mainFrame = Application.Current.MainWindow.FindName("MainWindowMainFrame") as Frame;

            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

            //if (mainFrame is not null && mainWindow is not null) { mainFrame.Content = mainWindow.MainPage; }

            if (mainWindow is not null) { Application.Current.MainWindow.Content = mainWindow.MainPage; }

        }

        private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            BackButton.IsEnabled = false;
            RunButton.IsEnabled = false;
            CancelButton.IsEnabled = true;


            Timer timer = new(CountDown, null, 0, 1000);

            ResetProgress();
            PercentageCompleted.Text = "0%";
            PendingTime.Text = "Pending Time: Calculating...";

            try
            {
                this.CancelTokenSource = new();

                await RunSolutionSecondThreadAsync();
            }
            catch (Exception ex)
            {
                UILog(LogInfo.ErrorNotification(ex.Message));
            }

            timer.Dispose();

            BackButton.IsEnabled = true;
            RunButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }

        // Reference: https://learn.microsoft.com/en-us/answers/questions/1045656/wpf-usercontrol-run-button-click-async
        private Task RunSolutionSecondThreadAsync()
        {
            return Task.Run(async() =>
            {
                await _solutionForm.RunSolutionAsync(UILog, CancelTokenSource);
            });
        }

        private void CancelButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            UILog(LogInfo.ErrorNotification("Canceling solution. Please wait while we stop all the processes."));
            this.CancelTokenSource.Cancel();
            this.CancelTokenSource.Dispose();
            CancelButton.IsEnabled = false;
        }

        private void FilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(SolutionFolder))
            {
                try { Process.Start("explorer.exe", @SolutionFolder); }
                catch (Exception ex)
                {
                    UILog(LogInfo.ErrorNotification(ex.Message));
                }
            };
        }

        private void UILog(LogInfo logInfo)
        {
            // Reference: https://stackoverflow.com/questions/2382663/ensuring-that-things-run-on-the-ui-thread-in-wpf
            rwl.AcquireWriterLock(3000);
            try
            {
                if (!string.IsNullOrWhiteSpace(logInfo.TextBase) || !string.IsNullOrWhiteSpace(logInfo.TextError) || !string.IsNullOrEmpty(logInfo.SolutionFolder))
                {
                    if (BoxText.Dispatcher.CheckAccess())
                    {
                        if (!string.IsNullOrWhiteSpace(logInfo.TextBase)) { BoxText.Inlines.Add(new Run($"{logInfo.TextBase} \n") ); }

                        if (!string.IsNullOrWhiteSpace(logInfo.TextError)) { BoxText.Inlines.Add(new Run($"{logInfo.TextError} \n") { Foreground = Brushes.IndianRed, FontWeight = FontWeights.Medium }); }

                    }
                    else
                    {
                        BoxText.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            if (!string.IsNullOrEmpty(logInfo.SolutionFolder)) { SolutionFolder = logInfo.SolutionFolder; }

                            if (!string.IsNullOrWhiteSpace(logInfo.TextBase)) { BoxText.Inlines.Add(new Run($"{logInfo.TextBase} \n") ); }

                            if (!string.IsNullOrWhiteSpace(logInfo.TextError)) { BoxText.Inlines.Add(new Run($"{logInfo.TextError} \n") { Foreground = Brushes.IndianRed, FontWeight = FontWeights.Medium }); }
                        }));
                    }
                }

                if (logInfo.PercentageProgress != -1)
                {
                    PendingTimeSpan = logInfo.PendingTime;

                    if (Progress.Dispatcher.CheckAccess())
                    {
                        Progress.Value = logInfo.PercentageProgress;
                        PercentageCompleted.Text = $"{logInfo.PercentageProgress}%";
                    }
                    else
                    {
                        Progress.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            Progress.Value = logInfo.PercentageProgress;
                            PercentageCompleted.Text = $"{logInfo.PercentageProgress}%";
                        }));
                    }
                }
            }

            finally
            {
                rwl.ReleaseLock();
            }

        }

        private void CountDown(object? state)
        {
            rwl.AcquireWriterLock(3000);
            try
            {
                
                if (PendingTimeSpan > TimeSpan.Zero)
                {
                    PendingTimeSpan = PendingTimeSpan.Subtract(TimeSpan.FromSeconds(1));
                }
            }
            finally
            {
                rwl.ReleaseWriterLock();
            } 
        }

    }
}
