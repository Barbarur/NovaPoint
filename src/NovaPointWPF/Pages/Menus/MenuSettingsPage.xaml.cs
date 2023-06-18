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

        public string Domain { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public bool CachingToken { get; set; } = false;

        public MenuSettingsPage()
        {
            InitializeComponent();

            DataContext = this;

            Domain = Properties.Settings.Default.Domain;
            TenantId = Properties.Settings.Default.TenantId;
            ClientId = Properties.Settings.Default.ClientId;
            CachingToken = Properties.Settings.Default.CachingToken;

            if (Properties.Settings.Default.IsUpdated) { UpdateButton.Visibility = Visibility.Collapsed; }
            else { UpdateButton.Visibility = Visibility.Visible; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Domain = Domain;
            Properties.Settings.Default.TenantId = TenantId;
            Properties.Settings.Default.ClientId = ClientId;
            Properties.Settings.Default.CachingToken = CachingToken;

            Properties.Settings.Default.Save();

            if (!CachingToken) { AppInfo.RemoveTokenCache(); }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            AppInfo.RemoveTokenCache();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Content = new AboutPage();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/releases/latest";
            //var url = NavigateUri.Replace("&", "^&");
            //Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            Process.Start(new ProcessStartInfo("cmd", $"/c start {NavigateUri}") { CreateNoWindow = true });
        }

    }
}
