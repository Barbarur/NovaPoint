using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using PnP.Framework.Diagnostics.Tree;
using PnP.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

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
        private string SolutionFolder2 = "C:\\Users\\ax_zi\\MEGA\\Coding Projects\\NovaPoint Project\\NovaPoint.wiki"; //string.Empty;



        //public string Notification { get; set; } = string.Empty;
        //public string PercentageCompleted { get; set; } = string.Empty;
        //public string PendingTime { get; set; } = string.Empty;
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
            //Progress.Value = 0;
            //PercentageCompleted.Content = String.Empty;
            //PendingTime.Content = String.Empty;
            //BoxText.Text = "";


            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.Domain) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.TenantId) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.ClientId))
            {
                LogInfo logInfo = new("Please go to Settings and fill the App Information");
                UILog(logInfo);
            }
            else
            {
                AppInfo appInfo = new(Properties.Settings.Default.Domain,
                    Properties.Settings.Default.TenantId,
                    Properties.Settings.Default.ClientId,
                    Properties.Settings.Default.CachingToken);

                this.CancelTokenSource = appInfo.CancelTokenSource;

                await RunSolutionSecondThreadAsync(appInfo);
                
            }

            BackButton.IsEnabled = true;
            RunButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }

        // Reference: https://learn.microsoft.com/en-us/answers/questions/1045656/wpf-usercontrol-run-button-click-async
        private Task RunSolutionSecondThreadAsync(AppInfo appInfo)
        {
            return Task.Run(async() =>
            {
                await _solutionForm.RunSolutionAsync(UILog, appInfo);
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
