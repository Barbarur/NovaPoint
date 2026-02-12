using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.SolutionsFormParameters
{

    public partial class AppClientSelectorForm : UserControl
    {
        private readonly AppConfig _appConfig;

        public AppClientSelectorForm()
        {
            InitializeComponent();

            _appConfig = AppConfig.GetSettings();

            List<IAppClientProperties> appProperties = [.. _appConfig.ListAppClientPublicProperties, .. _appConfig.ListAppClientConfidentialProperties];
            appProperties = [.. appProperties.OrderBy(p => p.ClientTitle)];

            ComboBoxAppProperties.ItemsSource = appProperties;
            ComboBoxAppProperties.DisplayMemberPath = "ClientTitle";
            ComboBoxAppProperties.SelectedValuePath = "ClientTitle";
            ComboBoxAppProperties.SelectedIndex = 0;

            if (appProperties.Count == 0)
            {
                SelectorPanel.Visibility = Visibility.Collapsed;
                NoAppNotification.Visibility = Visibility.Visible;
            }
        }

        private void ComboBoxAppProperties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxAppProperties.SelectedItem is IAppClientProperties properties)
            {
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
                NoAppNotification.Visibility = Visibility.Visible;
                NoAppNotification.Text = "This App is not correct.";
            }
        }

        internal IAppClientProperties GetClient()
        {
            if (ComboBoxAppProperties.SelectedItem is AppClientConfidentialProperties confidentialProperties)
            {
                SecureString securePassword = TextBoxCertificatePassword.SecurePassword;
                securePassword.MakeReadOnly();
                confidentialProperties.Password = securePassword;
                return confidentialProperties;
            }
            else if (ComboBoxAppProperties.SelectedItem is AppClientPublicProperties publicProperties)
            {
                return publicProperties;
            }
            else
            {
                throw new Exception("App properties is neither public or confidential. Please check your settings.");
            }

        }
    }
}
