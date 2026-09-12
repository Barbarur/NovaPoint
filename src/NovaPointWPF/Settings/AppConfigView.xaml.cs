using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Platform;
using NovaPointLibrary.Core.Settings;
using NovaPointWPF.Platform;
using NovaPointWPF.Settings.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;


namespace NovaPointWPF.Settings
{
    public partial class AppConfigView : Page
    {
        private readonly AppConfig _appConfig;
        private readonly IUrlLauncher _urlLauncher = new ShellUrlLauncher();
        public AppConfigView()
        {
            InitializeComponent();

            _appConfig = AppConfig.GetSettings();

            List<IAppClientProperties> appProperties = [.. _appConfig.ListAppClientPublicProperties, .. _appConfig.ListAppClientConfidentialProperties];
            appProperties = [.. appProperties.OrderBy(p => p.ClientTitle)];

            foreach (IAppClientProperties client in appProperties)
            {
                SettingsPanel.Children.Add(new PropertiesFormController(client, _appConfig, RemovePropertiesForm, isSaved: true));
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
            PropertiesFormController formController = new PropertiesFormController(properties, _appConfig, RemovePropertiesForm, isSaved: false);
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

        // Loaded has no Command surface in WPF without a behaviors library, so this stays an
        // async event handler - the one case Microsoft's own guidance excepts - and every
        // exception path is already caught internally.
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

        // Async void, deliberately: a Command binding here would fail silently at runtime if the
        // property name were ever wrong, for no real gain - this handler isn't reused across
        // frameworks, and the finally below already guarantees the button re-enables.
        private async void DeleteCacheClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null) { button.IsEnabled = false; }

            try
            {
                await Task.Run(() => AppConfig.RemoveTokenCache());
                TriggerNotification("Cache deleted");
            }
            finally
            {
                if (button != null) { button.IsEnabled = true; }
            }
        }

        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            _urlLauncher.Open("https://github.com/Barbarur/NovaPoint/releases/latest");
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
