using NovaPointLibrary.Solutions;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions
{
    public partial class SolutionPreparationPage : Page
    {
        static ReaderWriterLock rwl = new ReaderWriterLock();
        private readonly ISolutionForm _solutionForm;

        public SolutionPreparationPage(ISolutionForm solutionForm)
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = solutionForm.SolutionName;
            SolutionHeader.SolutionCode = solutionForm.SolutionCode;
            SolutionHeader.SolutionDocs = solutionForm.SolutionDocs;

            SolutionFormFrame.Content = solutionForm;

            _solutionForm = solutionForm;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow is not null) { Application.Current.MainWindow.Content = mainWindow.MainPage; }
        }

        private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            BackButton.IsEnabled = false;
            RunButton.IsEnabled = false;


            try
            {
                SolutionHandler handler = new(_solutionForm.SolutionCreate, _solutionForm.GetParameters(), AppSelector.GetClient());

                SolutionProgressView sViewer = new(handler);
                SolutionProgressViewFrame.Content = sViewer;

                await sViewer.RunSolutionAsync();
            }
            catch (Exception ex)
            {
                //UILog(LogInfo.ErrorNotification($"Exception: {ex.Message}"));
                //UILog(LogInfo.ErrorNotification($"StackTrace: {ex.StackTrace}"));
            }

            BackButton.IsEnabled = true;
            RunButton.IsEnabled = true;
        }

    }
}
