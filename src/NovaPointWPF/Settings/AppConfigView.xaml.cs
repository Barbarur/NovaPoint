using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Settings;
using NovaPointWPF.Settings.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;


namespace NovaPointWPF.Settings
{
    public partial class AppConfigView : Page
    {
        private readonly AppConfig _appConfig;
        public AppConfigView()
        {
            InitializeComponent();

            _appConfig = AppConfig.GetSettings();

            List<IAppClientProperties> appProperties = [.. _appConfig.ListPublicApps, .. _appConfig.ListConfidentialApps];
            appProperties = [.. appProperties.OrderBy(p => p.ClientTitle)];

            foreach (IAppClientProperties client in appProperties)
            {
                if (client is AppClientConfidentialProperties confidentialProperties)
                {
                    AppClientConfidentialPropertiesForm confidentialPropertiesForm = AppClientConfidentialPropertiesForm.GetExistingForm(confidentialProperties, _appConfig);
                    PropertiesFormController formController = PropertiesFormController.GetExistingForm(confidentialPropertiesForm, RemovePropertiesForm);
                    SettingsPanel.Children.Add(formController);
                }
                else if (client is AppClientPublicProperties publicProperties)
                {
                    AppClientPublicPropertiesForm publicPropertiesForm = AppClientPublicPropertiesForm.GetExistingForm(publicProperties, _appConfig);
                    PropertiesFormController formController = PropertiesFormController.GetExistingForm(publicPropertiesForm, RemovePropertiesForm);
                    SettingsPanel.Children.Add(formController);
                }
            }
        }

        private void AddAppClientPublicPropertiesFormClick(object sender, RoutedEventArgs e)
        {
            AppClientPublicPropertiesForm publicProperties = AppClientPublicPropertiesForm.GetNewForm(_appConfig);
            PropertiesFormController formController = PropertiesFormController.GetNewForm(publicProperties, RemovePropertiesForm);
            SettingsPanel.Children.Add(formController);
            SettingsScrollViewer.ScrollToEnd();
        }

        private void AddAppClientConfidentialPropertiesClick(object sender, RoutedEventArgs e)
        {
            AppClientConfidentialPropertiesForm confidentialProperties = AppClientConfidentialPropertiesForm.GetNewForm(_appConfig);
            PropertiesFormController formController = PropertiesFormController.GetNewForm(confidentialProperties, RemovePropertiesForm);
            SettingsPanel.Children.Add(formController);
            SettingsScrollViewer.ScrollToEnd();
        }
        private void RemovePropertiesForm(object? sender, EventArgs e)
        {
            if (sender is UserControl userControl)
            {
                SettingsPanel.Children.Remove(userControl);
            }
        }

        private async void CheckForUpdatesAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isUpdated = await VersionControl.IsUpdatedAsync();
                if (isUpdated) { UpdateButton.Visibility = Visibility.Collapsed; }
                else { UpdateButton.Visibility = Visibility.Visible; }
            }
            catch
            {
                UpdateErrorNotification.Visibility = Visibility.Visible;
            }
        }

        private void DeleteCacheClick(object sender, RoutedEventArgs e)
        {
            AppConfig.RemoveTokenCache();

            TriggerNotification("Cache deleted");
        }

        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/releases/latest";
            System.Diagnostics.Process.Start(new ProcessStartInfo("cmd", $"/c start {NavigateUri}") { CreateNoWindow = true });
        }

        private void TriggerNotification(string notification)
        {
            NotificationMessage.Text = notification;

            var storyboard = new Storyboard();

            // Create the fade-in animation
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.1),
                FillBehavior = FillBehavior.HoldEnd
            };

            // Create the hold animation
            var holdAnimation = new DoubleAnimation
            {
                From = 1,
                To = 1,
                BeginTime = TimeSpan.FromSeconds(0.1),
                Duration = TimeSpan.FromSeconds(1),
                FillBehavior = FillBehavior.HoldEnd
            };
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = TimeSpan.FromSeconds(1.1),
                Duration = TimeSpan.FromSeconds(1),
                FillBehavior = FillBehavior.HoldEnd
            };

            Storyboard.SetTarget(fadeInAnimation, NotificationMessage);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));

            Storyboard.SetTarget(holdAnimation, NotificationMessage);
            Storyboard.SetTargetProperty(holdAnimation, new PropertyPath("Opacity"));

            Storyboard.SetTarget(fadeOutAnimation, NotificationMessage);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(holdAnimation);
            storyboard.Children.Add(fadeOutAnimation);

            storyboard.Begin();
        }

    }
}
