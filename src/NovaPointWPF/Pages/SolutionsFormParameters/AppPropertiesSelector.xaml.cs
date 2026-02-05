using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.SolutionsFormParameters
{

    public partial class AppPropertiesSelector : UserControl
    {
        private readonly AppConfig _appConfig;

        public IAppClientProperties? AppClient = null;

        public AppPropertiesSelector()
        {
            InitializeComponent();

            _appConfig = AppConfig.GetSettings();

            var appProperties = new List<IAppClientProperties>();
            appProperties.AddRange(_appConfig.ListPublicApps);
            appProperties.AddRange(_appConfig.ListConfidentialApps);
            ComboBoxAppProperties.ItemsSource = appProperties;
            ComboBoxAppProperties.DisplayMemberPath = "ClientTitle";
            ComboBoxAppProperties.SelectedValuePath = "ClientTitle";
            ComboBoxAppProperties.SelectedIndex = 0;

            if (!appProperties.Any())
            {
                NoAppNotification.Visibility = Visibility.Visible;
            }
        }

        private void ComboBoxAppProperties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxAppProperties.SelectedItem is IAppClientProperties properties)
            {
                AppClient = properties;
                NoAppNotification.Visibility = Visibility.Collapsed;
                if (ComboBoxAppProperties.SelectedItem is AppClientConfidentialProperties)
                {
                    CertificatePasswordPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CertificatePasswordPanel.Visibility = Visibility.Collapsed;
                    TextBoxCertificatePassword.Password = string.Empty;
                }
            }
            else
            {
                AppClient = null;
                NoAppNotification.Visibility = Visibility.Visible;
                NoAppNotification.Text = "This App is not correct.";
            }
        }
    }
}
