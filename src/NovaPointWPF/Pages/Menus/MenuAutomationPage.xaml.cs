using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.Automation;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Menus
{
    /// <summary>
    /// Interaction logic for MenuAutomationPage.xaml
    /// </summary>
    public partial class MenuAutomationPage : Page
    {
        public MenuAutomationPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionForm solutionForm)
        {
            Application.Current.MainWindow.Content = new SolutionPreparationPage(solutionForm);
        }


        // SITES
        private void GoSetSiteCollectionAdminAutoForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SetSiteCollectionAdminAutoForm());
        }

        private void GoRemoveSiteAutoForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveSiteAutoForm());
        }


        // LISTS
        private void SetVersioningLimitAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SetVersioningLimitAutoForm());
        }


        // ITEMS
        private void CopyDuplicateFileAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new CopyDuplicateFileAutoForm());
        }
        private void CheckInFileAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new CheckInFileAutoForm());
        }
        private void RemoveFileVersionAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveFileVersionAutoForm());
        }
        private void RestorePHLItemAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RestorePHLItemAutoForm());
        }
        private void RemovePHLItemAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemovePHLItemAutoForm());
        }


        // RECYCLE BIN
        private void RestoreRecycleBinAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RestoreRecycleBinAutoForm());
        }

        private void ClearRecycleBinAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ClearRecycleBinAutoForm());
        }


        // USER
        private void GoRemoveSiteUserAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveSiteUserAutoForm());
        }

        private void GoRemoveSharingLinksAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveSharingLinksAutoForm());
        }

    }
}
