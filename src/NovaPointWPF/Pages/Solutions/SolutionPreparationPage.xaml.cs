using CommunityToolkit.Mvvm.Input;
using NovaPointLibrary.Solutions;
using NovaPointViewModels;
using NovaPointWPF.Platform;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions
{
    public partial class SolutionPreparationPage : Page
    {
        private readonly INavigationService _navigationService = new WpfNavigationService();
        private readonly ISolutionViewModel _solutionForm;

        public IAsyncRelayCommand RunCommand { get; }

        public SolutionPreparationPage(ISolutionViewModel solutionForm)
        {
            InitializeComponent();

            DataContext = this;

            RunCommand = new AsyncRelayCommand(RunSolutionAsync);

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

        private async Task RunSolutionAsync()
        {
            BackButton.IsEnabled = false;
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
                MessageBox.Show($"{ex.Message}", "NovaPoint - Error starting the solution", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BackButton.IsEnabled = true;
                StackPanelForm.IsEnabled = true;
            }
        }

    }
}
