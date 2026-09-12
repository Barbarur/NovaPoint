using NovaPointLibrary.Commands.Utilities;
using NovaPointViewModels;
using NovaPointWPF.Platform;
using NovaPointWPF.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NovaPointWPF.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private readonly INavigationService _navigationService;

        public MainPage()
        {
            InitializeComponent();

            _navigationService = new WpfNavigationService(SolutionListFrame);
        }

        private async void CheckForUpdatesAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isUpdated = await VersionControl.IsUpdatedAsync();
                if (!isUpdated) { SettingsButton.Background = Brushes.DarkRed; }
                else { SettingsButton.ClearValue(Button.BackgroundProperty) ; }
            }
            catch
            {
                SettingsButton.Background = Brushes.DarkRed;
            }
        }

        private void Directory_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new Pages.Menus.MenuDirectoryPage());
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new Pages.Menus.MenuReportPage());
        }

        private void QuickFix_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new Pages.Menus.MenuQuickFixPage());
        }

        private void Automation_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new Pages.Menus.MenuAutomationPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new AppConfigView());
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo(new AboutPage());
        }
    }
}
