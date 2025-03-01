using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientPublicPropertiesForm : UserControl, IPropertiesForm
    {
        private readonly AppClientPublicProperties _properties;
        private AppConfig _appConfig;

        private AppClientPublicPropertiesForm(AppClientPublicProperties properties, AppConfig appConfig)
        {
            InitializeComponent();

            DataContext = properties;

            _properties = properties;
            _appConfig = appConfig;
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
            TextBoxAppTitle.IsReadOnly = false;
            TextBoxAppTenantId.IsReadOnly = false;
            TextBoxAppClientId.IsReadOnly = false;
            ButtonAppCache.IsEnabled = true;
        }

        private void DisableForm()
        {
            TextBoxAppTitle.IsReadOnly = true;
            TextBoxAppTenantId.IsReadOnly = true;
            TextBoxAppClientId.IsReadOnly = true;
            ButtonAppCache.IsEnabled = false;
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
