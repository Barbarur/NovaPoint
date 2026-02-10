using NovaPointLibrary.Solutions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions
{
    public partial class SolutionProgressView : UserControl
    {
        public SolutionHandler Handler;

        public SolutionProgressView(SolutionHandler handler)
        {
            DataContext = handler;
            Handler = handler;

            InitializeComponent();
        }

        internal async Task RunSolutionAsync()
        {
            CancelButton.IsEnabled = true;
            FolderButton.IsEnabled = false;

            try
            {
                await Handler.RunSolution();
            }
            catch (Exception ex)
            {
                Handler.UILog(LogInfo.ErrorNotification(ex.Message));
            }

            Handler.PendingTime = "Completed!";
            FolderButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Handler.UILog(LogInfo.ErrorNotification("Canceling solution. Please wait while we stop all the processes."));
            Handler.CancelTokenSource.Cancel();
            Handler.CancelTokenSource.Dispose();
            CancelButton.IsEnabled = false;
        }

        private void FolderClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(Handler.SolutionFolder))
            {
                try { Process.Start("explorer.exe", Handler.SolutionFolder); }
                catch (Exception ex)
                {
                    Handler.UILog(LogInfo.ErrorNotification(ex.Message));
                }
            }
            ;
        }
    }
}
