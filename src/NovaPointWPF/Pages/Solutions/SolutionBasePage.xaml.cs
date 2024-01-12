using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NovaPointWPF.Pages.Solutions
{
    /// <summary>
    /// Interaction logic for SolutionBasePage.xaml
    /// </summary>
    public partial class SolutionBasePage : Page
    {
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
            PercentageCompleted.Content = "Percentage completed";
            PendingTime.Content = "Pending time to complete";
            BoxText.Text = String.Empty;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame? mainFrame = Application.Current.MainWindow.FindName("MainWindowMainFrame") as Frame;

            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainFrame is not null && mainWindow is not null) { mainFrame.Content = mainWindow.MainPage; }

        }

        private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            BackButton.IsEnabled = false;
            RunButton.IsEnabled = false;
            CancelButton.IsEnabled = true;

            ResetProgress();

            try
            {
                this.CancelTokenSource = new();

                await RunSolutionSecondThreadAsync();
            }
            catch (Exception ex)
            {
                LogInfo log = new(ex.Message);
                UILog(log);
            }

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
                    LogInfo logInfo = new(ex.Message);
                    UILog(logInfo);
                }
            };
        }

        private void UILog(LogInfo logInfo)
        {
            // https://stackoverflow.com/questions/2382663/ensuring-that-things-run-on-the-ui-thread-in-wpf
            if (BoxText.Dispatcher.CheckAccess())
            {
                if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { BoxText.Text = BoxText.Text + logInfo.MainClassInfo + "\n"; }
                if (logInfo.PercentageProgress != -1)
                {
                    Progress.Value = logInfo.PercentageProgress;
                    PercentageCompleted.Content = $"{logInfo.PercentageProgress}%";
                    if ( !string.IsNullOrWhiteSpace(logInfo.PendingTime))
                    {
                        PendingTime.Content = $"{logInfo.PendingTime}";
                    }
                }
            }
            else
            {
                BoxText.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    if (!string.IsNullOrEmpty(logInfo.SolutionFolder)) { SolutionFolder = logInfo.SolutionFolder; }
                    if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { BoxText.Text = BoxText.Text + logInfo.MainClassInfo + "\n"; }
                    if (logInfo.PercentageProgress != -1)
                    {
                        Progress.Value = logInfo.PercentageProgress;
                        PercentageCompleted.Content = $"{logInfo.PercentageProgress}%";
                        if (!string.IsNullOrWhiteSpace(logInfo.PendingTime))
                        {
                            PendingTime.Content = $"{logInfo.PendingTime}";
                        }
                    }
                }));
            }
        }
    }
}
