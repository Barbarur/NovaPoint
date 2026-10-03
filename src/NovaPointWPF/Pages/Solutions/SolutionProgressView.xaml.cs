using NovaPointLibrary.Core.Platform;
using NovaPointLibrary.Solutions;
using NovaPointWPF.Platform;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;


namespace NovaPointWPF.Pages.Solutions
{
    public partial class SolutionProgressView : UserControl
    {
        public SolutionHandler Handler;
        private readonly IFolderRevealer _folderRevealer = new OsFolderRevealer();

        // A TextBlock re-lays out every inline on each append, so the pane keeps only the newest lines.
        private const int MaxLogLines = 300;
        private Run? _trimNotice;

        public SolutionProgressView(SolutionHandler handler)
        {
            DataContext = handler;
            Handler = handler;

            InitializeComponent();

            Handler.LogLineAdded += AppendLogLine;
        }

        // The handler raises this on the UI thread, one call per line.
        private void AppendLogLine(LogInfo logInfo)
        {
            Run line = new($"{logInfo.Text} \n");

            if (logInfo.Type == LogInfoType.Error)
            {
                line.Foreground = Brushes.IndianRed;
                line.FontWeight = FontWeights.Medium;
            }

            BoxText.Inlines.Add(line);
            TrimLogLines();
        }

        private void TrimLogLines()
        {
            int limit = _trimNotice == null ? MaxLogLines : MaxLogLines + 1;
            if (BoxText.Inlines.Count <= limit) { return; }

            if (_trimNotice == null)
            {
                _trimNotice = new($"Showing the latest {MaxLogLines} lines only. The full log is in _Logs.txt in the output folder. \n")
                {
                    FontStyle = FontStyles.Italic
                };
                BoxText.Inlines.InsertBefore(BoxText.Inlines.FirstInline, _trimNotice);
            }

            BoxText.Inlines.Remove(_trimNotice.NextInline);
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
            Handler.Cancel();
            CancelButton.IsEnabled = false;
        }

        private void FolderClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(Handler.SolutionFolder))
            {
                try { _folderRevealer.Reveal(Handler.SolutionFolder); }
                catch (Exception ex)
                {
                    Handler.UILog(LogInfo.ErrorNotification(ex.Message));
                }
            }
            ;
        }
    }
}
