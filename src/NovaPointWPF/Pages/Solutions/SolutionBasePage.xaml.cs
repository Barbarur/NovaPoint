using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using PnP.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public string Notification { get; set; } = "";
        public CancellationTokenSource CancelTokenSource { get; set; }

        public SolutionBasePage(ISolutionForm solutionForm)
        {
            InitializeComponent();

            DataContext = this;

            SolutionFormFrame.Content = solutionForm;

            _solutionForm = solutionForm;

            Progress.Value = 0;
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

            BoxText.Text = "";
            Progress.Value = 0;

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.Domain) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.TenantId) ||
                string.IsNullOrWhiteSpace(Properties.Settings.Default.ClientId))
            {
                LogInfo logInfo = new(GetType().Name, mainInfo: "Please go to Settings and fill the App Information");
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
        }

        private void UILog(LogInfo logInfo)
        {
            // https://stackoverflow.com/questions/2382663/ensuring-that-things-run-on-the-ui-thread-in-wpf
            if (BoxText.Dispatcher.CheckAccess())
            {
                if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { BoxText.Text = BoxText.Text + logInfo.MainClassInfo + "\n"; }
                if (logInfo.PercentageProgress != 0) { Progress.Value = logInfo.PercentageProgress; }
            }
            else
            {
                BoxText.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { BoxText.Text = BoxText.Text + logInfo.MainClassInfo + "\n"; }
                    if (logInfo.PercentageProgress != 0) { Progress.Value = logInfo.PercentageProgress; }
                }));
            }
        }
    }
}
