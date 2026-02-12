using Microsoft.Graph.ExternalConnectors;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{

    public partial class PropertiesFormController : UserControl
    {
        private readonly IPropertiesForm _propertiesForm;
        private readonly AppConfig _appConfig;
        private readonly EventHandler _removeElement;

        public PropertiesFormController(IAppClientProperties properties, AppConfig appConfig, EventHandler removeElement)
        {
            InitializeComponent();

            IPropertiesForm propertiesForm;

            if (properties is AppClientConfidentialProperties confidentialProperties)
            {
                propertiesForm = AppClientConfidentialPropertiesForm.GetExistingForm(confidentialProperties, appConfig);
            }
            else if (properties is AppClientPublicProperties publicProperties)
            {
                propertiesForm = AppClientPublicPropertiesForm.GetExistingForm(publicProperties, appConfig);
            }
            else
            {
                throw new Exception("App properties is neither public or confidential. Please check your settings.");
            }
            _propertiesForm = propertiesForm;
            GridPropertiesForm.Children.Add((UIElement)propertiesForm);

            _appConfig = appConfig;
            _removeElement = removeElement;
        }

        internal void EnableForm()
        {
            ButtonEdit.Visibility = Visibility.Collapsed;
            ButtonEdit.IsEnabled = false;

            _propertiesForm.EnableForm();

            ButtonSave.Visibility = Visibility.Visible;
            ButtonSave.IsEnabled = true;

            ButtonDelete.Visibility = Visibility.Visible;
            ButtonDelete.IsEnabled = true;
        }

        internal void DisableForm()
        {
            ButtonEdit.Visibility = Visibility.Visible;
            ButtonEdit.IsEnabled = true;

            _propertiesForm.DisableForm();

            ButtonSave.Visibility = Visibility.Collapsed;
            ButtonSave.IsEnabled = false;

            ButtonDelete.Visibility = Visibility.Collapsed;
            ButtonDelete.IsEnabled = false;
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            EnableForm();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _appConfig.SaveSettings(_propertiesForm.Properties);
                DisableForm();
                TextBlockErrorNotification.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                TextBlockErrorNotification.Text = ex.Message;
                TextBlockErrorNotification.Visibility = Visibility.Visible;
            }
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            _removeElement.Invoke(this, EventArgs.Empty);
            _appConfig.RemoveApp(_propertiesForm.Properties);
        }
    }
}
