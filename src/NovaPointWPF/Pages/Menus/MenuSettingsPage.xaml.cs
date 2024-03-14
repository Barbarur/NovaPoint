using NovaPointLibrary.Commands.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace NovaPointWPF.Pages.Menus
{
    /// <summary>
    /// Interaction logic for MenuSettingsPage.xaml
    /// </summary>
    public partial class MenuSettingsPage : Page
    {
        public AppSettings AppSettings;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public bool CachingToken { get; set; } = false;

        public MenuSettingsPage()
        {
            InitializeComponent();

            DataContext = this;

            AppSettings = AppSettings.GetSettings();
            TenantId = AppSettings.TenantID;
            ClientId = AppSettings.ClientId;
            CachingToken = AppSettings.CachingToken;

            if (AppSettings.IsUpdated) { UpdateButton.Visibility = Visibility.Collapsed; }
            else { UpdateButton.Visibility = Visibility.Visible; }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            AppSettings.TenantID = TenantId;
            AppSettings.ClientId = ClientId;
            AppSettings.CachingToken = CachingToken;

            AppSettings.SaveSettings();

            if (!CachingToken) { AppSettings.RemoveTokenCache(); }
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            AppSettings.RemoveTokenCache();
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Content = new AboutPage();
        }

        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/releases/latest";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {NavigateUri}") { CreateNoWindow = true });
        }

    }
}
