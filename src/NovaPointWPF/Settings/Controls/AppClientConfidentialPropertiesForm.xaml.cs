using Microsoft.Win32;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientConfidentialPropertiesForm : UserControl, IPropertiesForm
    {
        public IAppClientProperties Properties { get; init; }
        private AppClientPropertiesCoreForm _corePropertiesForm;

        private AppClientConfidentialPropertiesForm(AppClientConfidentialProperties properties, AppConfig appConfig)
        {
            InitializeComponent();

            DataContext = properties;

            Properties = properties;

            _corePropertiesForm = new(properties);
            FormPanel.Children.Insert(0, _corePropertiesForm);
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
            _corePropertiesForm.EnableForm();
            ButtonAppCertificate.IsEnabled = true;
        }

        public void DisableForm()
        {
            _corePropertiesForm.DisableForm();
            ButtonAppCertificate.IsEnabled = false;
        }

        private void OpenCertificatePathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                CertificatePathTextBlock.Text = openFileDialog.FileName;
        }

    }
}
