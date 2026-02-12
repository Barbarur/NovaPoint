using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientPublicPropertiesForm : UserControl, IPropertiesForm
    {
        public IAppClientProperties Properties { get; init; }
        private AppClientPropertiesCoreForm _corePropertiesForm;

        private AppClientPublicPropertiesForm(AppClientPublicProperties properties, AppConfig appConfig)
        {
            InitializeComponent();

            DataContext = properties;

            Properties = properties;

            _corePropertiesForm = new(properties);
            FormPanel.Children.Insert(0, _corePropertiesForm);
        }

        public static AppClientPublicPropertiesForm GetNewForm(AppConfig appConfig)
        {
            AppClientPublicPropertiesForm form = new(new(), appConfig);
            form.EnableForm();
            return form;
        }

        public static AppClientPublicPropertiesForm GetExistingForm(AppClientPublicProperties properties, AppConfig appConfig)
        {
            AppClientPublicPropertiesForm form = new(properties, appConfig);
            form.DisableForm();
            return form;
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
