using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
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

namespace NovaPointWPF.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();

            string version = VersionControl.GetVersion();
            if (String.IsNullOrWhiteSpace(version)) { VersionNo.Content = String.Empty; }
            else { VersionNo.Content = "v " + version; }
        }

        private void GoToGitHub(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void GoToDocumentation(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/wiki";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        
        private void GoToLinkedIn(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://www.linkedin.com/company/novapointopen";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        
        private void GoToFund(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://buymeacoffee.com/novapoint";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

    }
}
