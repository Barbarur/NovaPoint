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


        private void GoToDocsClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/wiki";
            OpenBrowser(NavigateUri);
        }

        private void GoToYoutubeClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://www.youtube.com/@NovaPoint22";
            OpenBrowser(NavigateUri);
        }

        private void GoToGitHubClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint";
            OpenBrowser(NavigateUri);
        }
        
        private void GoToLinkedInClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://www.linkedin.com/company/novapointopen";
            OpenBrowser(NavigateUri);
        }

        private void GoToTwitterClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://x.com/NovaPoint22";
            OpenBrowser(NavigateUri);
        }

        private void GoToBlueskyClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://bsky.app/profile/novapoint.bsky.social";
            OpenBrowser(NavigateUri);
        }

        private void GoToMediumClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://novapoint.medium.com/";
            OpenBrowser(NavigateUri);
        }

        private void GoToFundClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://buymeacoffee.com/novapoint";
            OpenBrowser(NavigateUri);
        }

        private void OpenBrowser(string navigateUri)
        {
            var url = navigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

    }
}
