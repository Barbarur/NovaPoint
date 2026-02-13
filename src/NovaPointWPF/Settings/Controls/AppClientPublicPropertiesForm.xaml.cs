using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientPublicPropertiesForm : UserControl, IPropertiesForm
    {
        public IAppClientProperties Properties { get; init; }
        private AppClientPropertiesCoreForm _corePropertiesForm;

        internal AppClientPublicPropertiesForm(AppClientPublicProperties properties)
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
            ButtonAppCache.IsEnabled = true;
        }

        public void DisableForm()
        {
            _corePropertiesForm.DisableForm();
            ButtonAppCache.IsEnabled = false;
        }

    }
}
