using NovaPointLibrary.Solutions;
using NovaPointViewModels;
using NovaPointWPF.Platform;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions
{
    public partial class SolutionPreparationPage : Page
    {
        private readonly INavigationService _navigationService = new WpfNavigationService();
        private readonly ISolutionViewModel _solutionForm;

        public SolutionPreparationPage(ISolutionViewModel solutionForm)
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
            _navigationService.GoBack();
        }

        private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            BackButton.IsEnabled = false;
            RunButton.IsEnabled = false;
            StackPanelForm.IsEnabled = false;


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
            StackPanelForm.IsEnabled = true;
        }

    }
}
