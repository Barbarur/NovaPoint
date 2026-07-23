using NovaPointLibrary.Core.Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
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
            string logFile = WriteCrashLog(ex);

            MessageBox.Show(
                $"An unexpected error occurred and has been logged.\n\n{ex.GetType().Name}: {ex.Message}\n\nLog file: {logFile}",
                "NovaPoint - Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static string WriteCrashLog(Exception ex)
        {
            string folder = Path.Combine(AppFolders.GetOutputFolder(), "CrashReport");
            string logFile = Path.Combine(folder, $"{DateTime.Now:yyMMddHHmmss}WPFCrash.Log");

            try
            {
                Directory.CreateDirectory(folder);
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Best-effort logging only; a failure here should not throw again.
            }

            return logFile;
        }
    }
}
