using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{

    public partial class PropertiesFormController : UserControl
    {
        private IPropertiesForm _propertiesForm;
        private readonly AppConfig _appConfig;
        private readonly EventHandler _removeElement;

        public PropertiesFormController(IAppClientProperties properties, AppConfig appConfig, EventHandler removeElement)
        {
            InitializeComponent();

            //IPropertiesForm propertiesForm;

            //if (properties is AppClientConfidentialProperties confidentialProperties)
            //{
            //    propertiesForm = new AppClientConfidentialPropertiesForm(confidentialProperties.Clone(), appConfig);
            //}
            //else if (properties is AppClientPublicProperties publicProperties)
            //{
            //    propertiesForm = new AppClientPublicPropertiesForm(publicProperties.Clone(), appConfig);
            //}
            //else
            //{
            //    throw new Exception("App properties is neither public or confidential. Please check your settings.");
            //}
            //GridPropertiesForm.Children.Add((UIElement)propertiesForm);

            _propertiesForm = AddChildrenForm(properties);
            _appConfig = appConfig;
            _removeElement = removeElement;
        }

        private IPropertiesForm AddChildrenForm(IAppClientProperties properties)
        {
            IPropertiesForm propertiesForm;

            if (properties is AppClientConfidentialProperties confidentialProperties)
            {
                propertiesForm = new AppClientConfidentialPropertiesForm(confidentialProperties.Clone());
            }
            else if (properties is AppClientPublicProperties publicProperties)
            {
                propertiesForm = new AppClientPublicPropertiesForm(publicProperties.Clone());
            }
            else
            {
                throw new Exception("App properties is neither public or confidential. Please check your settings.");
            }

            GridPropertiesForm.Children.Add((UIElement)propertiesForm);
            return propertiesForm;
        }

        internal void EnableForm()
        {
            ButtonEdit.Visibility = Visibility.Collapsed;
            ButtonEdit.IsEnabled = false;

            _propertiesForm.EnableForm();

            PanelActions.Visibility = Visibility.Visible;
            ButtonSave.IsEnabled = true;
            ButtonCancel.IsEnabled = true;
            ButtonDelete.IsEnabled = true;
        }

        internal void DisableForm()
        {
            ButtonEdit.Visibility = Visibility.Visible;
            ButtonEdit.IsEnabled = true;

            _propertiesForm.DisableForm();

            PanelActions.Visibility = Visibility.Collapsed;
            ButtonSave.IsEnabled = false;
            ButtonCancel.IsEnabled = false;
            ButtonDelete.IsEnabled = false;

            TextBlockErrorNotification.Visibility = Visibility.Collapsed;
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
            }
            catch (Exception ex)
            {
                TextBlockErrorNotification.Text = ex.Message;
                TextBlockErrorNotification.Visibility = Visibility.Visible;
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                GridPropertiesForm.Children.Clear();
                IAppClientProperties originalProperties = _appConfig.GetOriginalSettings(_propertiesForm.Properties);
                _propertiesForm = AddChildrenForm(originalProperties);
                DisableForm();
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
