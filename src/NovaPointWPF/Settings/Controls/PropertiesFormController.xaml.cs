using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{

    public partial class PropertiesFormController : UserControl
    {
        private IPropertiesForm _propertiesForm;
        private readonly AppConfig _appConfig;
        private readonly EventHandler _removeElement;
        private bool _isSaved;

        public PropertiesFormController(IAppClientProperties properties, AppConfig appConfig, EventHandler removeElement, bool isSaved)
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
            _isSaved = isSaved;
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

        // Async void, deliberately: a Command binding here would fail silently at runtime if the
        // property name were ever wrong, for no real gain - this handler isn't reused across
        // frameworks, and every exception path below is already caught internally.
        private async void SaveClick(object sender, RoutedEventArgs e)
        {
            ButtonSave.IsEnabled = false;
            try
            {
                await Task.Run(() => _appConfig.SaveSettings(_propertiesForm.Properties));
                _isSaved = true;
                DisableForm();
            }
            catch (Exception ex)
            {
                TextBlockErrorNotification.Text = ex.Message;
                TextBlockErrorNotification.Visibility = Visibility.Visible;
            }
            finally
            {
                ButtonSave.IsEnabled = true;
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            if (!_isSaved)
            {
                _removeElement.Invoke(this, EventArgs.Empty);
                return;
            }

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

        // Async void, deliberately - see SaveClick above.
        private async void DeleteClick(object sender, RoutedEventArgs e)
        {
            ButtonDelete.IsEnabled = false;
            try
            {
                await Task.Run(() => _appConfig.RemoveApp(_propertiesForm.Properties));
                _removeElement.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                TextBlockErrorNotification.Text = ex.Message;
                TextBlockErrorNotification.Visibility = Visibility.Visible;
            }
            finally
            {
                ButtonDelete.IsEnabled = true;
            }
        }
    }
}
