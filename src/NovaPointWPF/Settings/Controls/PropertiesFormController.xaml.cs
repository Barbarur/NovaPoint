using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{

    public partial class PropertiesFormController : UserControl
    {
        private readonly IPropertiesForm _properties;
        private readonly EventHandler _removeElement;

        private PropertiesFormController(IPropertiesForm properties, EventHandler removeElement)
        {
            InitializeComponent();
            _properties = properties;
            GridPropertiesForm.Children.Insert(0, (UIElement)properties);

            _removeElement = removeElement;
        }

        public static PropertiesFormController GetNewForm(IPropertiesForm properties, EventHandler removeElement)
        {
            PropertiesFormController form = new(properties, removeElement);
            form.EnableForm();
            return form;
        }

        public static PropertiesFormController GetExistingForm(IPropertiesForm properties, EventHandler removeElement)
        {
            PropertiesFormController form = new(properties, removeElement);
            form.DisableForm();
            return form;
        }

        private void EnableForm()
        {
            _properties.EnableForm();

            ButtonSave.Visibility = Visibility.Visible;
            ButtonSave.IsEnabled = true;

            ButtonEdit.Visibility = Visibility.Collapsed;
            ButtonEdit.IsEnabled = false;
        }

        private void DisableForm()
        {
            ButtonSave.Visibility = Visibility.Collapsed;
            ButtonSave.IsEnabled = false;

            ButtonEdit.Visibility = Visibility.Visible;
            ButtonEdit.IsEnabled = true;
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            EnableForm();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _properties.SaveForm();
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
            _removeElement?.Invoke(this, EventArgs.Empty);
            _properties.DeleteForm();
        }
    }
}
