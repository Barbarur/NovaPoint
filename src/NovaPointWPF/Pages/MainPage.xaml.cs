using NovaPointLibrary.Commands.Authentication;
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
        public MainPage()
        {
            InitializeComponent();
            CheckUpdate();
        }

        private void CheckUpdate()
        {
            var appSettings = AppSettings.GetSettings();
            if (!appSettings.IsUpdated) { SettingsButton.Background = Brushes.DarkRed; }
        }

        private async void CheckUpdateAsync(object sender, RoutedEventArgs e)
        {
            await AppSettings.CheckForUpdatesAsync();
            CheckUpdate();
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            SolutionListFrame.Content = new Pages.Menus.MenuReportPage();
        }

        private void QuickFix_Click(object sender, RoutedEventArgs e)
        {
            SolutionListFrame.Content = new Pages.Menus.MenuQuickFixPage();
        }

        private void Automation_Click(object sender, RoutedEventArgs e)
        {
            SolutionListFrame.Content = new Pages.Menus.MenuAutomationPage();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
            SolutionListFrame.Content = new Pages.Menus.MenuSettingsPage();
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            SolutionListFrame.Content = new AboutPage();
        }
    }
}
