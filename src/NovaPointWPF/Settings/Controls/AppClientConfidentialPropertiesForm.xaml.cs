using Microsoft.Win32;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientConfidentialPropertiesForm : UserControl, IPropertiesForm
    {
        private readonly AppClientConfidentialProperties _properties;
        private readonly AppConfig _appConfig;

        private AppClientConfidentialPropertiesForm(AppClientConfidentialProperties properties, AppConfig appConfig)
        {
            InitializeComponent();

            DataContext = properties;

            _properties = properties;
            _appConfig = appConfig;
        }

        public static AppClientConfidentialPropertiesForm GetNewForm(AppConfig appConfig)
        {
            AppClientConfidentialPropertiesForm form = new(new(), appConfig);
            form.EnableForm();
            return form;
        }

        public static AppClientConfidentialPropertiesForm GetExistingForm(AppClientConfidentialProperties properties, AppConfig appConfig)
        {
            AppClientConfidentialPropertiesForm form = new(properties, appConfig);
            form.DisableForm();
            return form;
        }

        public void EnableForm()
        {
            TextBoxAppTitle.IsReadOnly = false;
            TextBoxAppTenantId.IsReadOnly = false;
            TextBoxAppClientId.IsReadOnly = false;
            ButtonAppCertificate.IsEnabled = true;
        }

        private void DisableForm()
        {
            TextBoxAppTitle.IsReadOnly = true;
            TextBoxAppTenantId.IsReadOnly = true;
            TextBoxAppClientId.IsReadOnly = true;
            ButtonAppCertificate.IsEnabled = false;
        }

        private void OpenCertificatePathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                CertificatePathTextBlock.Text = openFileDialog.FileName;
        }

        public void SaveForm()
        {
            _appConfig.SaveSettings(_properties);
            DisableForm();
        }

        public void DeleteForm()
        {
            _appConfig.RemoveApp(_properties);
        }
    }
}
