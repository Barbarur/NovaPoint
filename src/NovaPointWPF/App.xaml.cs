using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Core.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NovaPointWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Off the UI thread and after the window is up; a recursive delete of legacy data can take
            // seconds and nothing in the app reads what is being removed.
            Task.Run(() =>
            {
                try
                {
                    AppFolders.CleanUpLegacyFolders();
                }
                catch (Exception ex)
                {
                    // A failed clean up is not user actionable, so it is logged instead of shown.
                    LogCrash.WriteCrashLog(ex, "CleanUp");
                }
            });
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogAndShow(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex) { LogAndShow(ex); }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogAndShow(e.Exception);
            e.SetObserved();
        }

        private static void LogAndShow(Exception ex)
        {
            string logFile = LogCrash.WriteCrashLog(ex, "WPFCrash");

            MessageBox.Show(
                $"An unexpected error occurred and has been logged.\n\n{ex.GetType().Name}: {ex.Message}\n\nLog file: {logFile}",
                "NovaPoint - Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
