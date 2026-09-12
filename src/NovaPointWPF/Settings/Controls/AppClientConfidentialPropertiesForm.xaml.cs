using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Platform;
using NovaPointLibrary.Core.Settings;
using NovaPointWPF.Platform;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientConfidentialPropertiesForm : UserControl, IPropertiesForm
    {
        private readonly IFilePicker _filePicker = new WpfFilePicker();

        public IAppClientProperties Properties { get; init; }
        private AppClientPropertiesCoreForm _corePropertiesForm;

        internal AppClientConfidentialPropertiesForm(AppClientConfidentialProperties properties)
        {
            InitializeComponent();

            DataContext = properties;

            Properties = properties;

            _corePropertiesForm = new(properties);
            FormPanel.Children.Insert(0, _corePropertiesForm);
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
            // WpfFilePicker's Task completes synchronously (the dialog itself blocks), so this
            // never actually waits on anything - safe without an async handler.
            string? path = _filePicker.PickFileAsync(title: "", filter: "").GetAwaiter().GetResult();
            if (path is not null)
                CertificatePathTextBlock.Text = path;
        }

    }
}
