using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
