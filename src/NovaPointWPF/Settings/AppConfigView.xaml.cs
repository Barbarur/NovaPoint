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

            List<IAppClientProperties> appProperties = [.. _appConfig.ListAppClientPublicProperties, .. _appConfig.ListAppClientConfidentialProperties];
            appProperties = [.. appProperties.OrderBy(p => p.ClientTitle)];

            foreach (IAppClientProperties client in appProperties)
            {
                SettingsPanel.Children.Add(new PropertiesFormController(client, _appConfig, RemovePropertiesForm));
            }
        }

        private void AddAppClientPublicPropertiesFormClick(object sender, RoutedEventArgs e)
        {
            AddNewAppClientForm(new AppClientPublicProperties());
        }

        private void AddAppClientConfidentialPropertiesClick(object sender, RoutedEventArgs e)
        {
            AddNewAppClientForm(new AppClientConfidentialProperties());
        }

        private void AddNewAppClientForm(IAppClientProperties properties)
        {
            PropertiesFormController formController = new PropertiesFormController(properties, _appConfig, RemovePropertiesForm);
            formController.EnableForm();
            SettingsPanel.Children.Insert(0, formController);
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
