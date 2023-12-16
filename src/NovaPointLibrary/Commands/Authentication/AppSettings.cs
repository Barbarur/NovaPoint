using NovaPointLibrary.Commands.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NovaPointLibrary.Commands.Authentication
{
    public class AppSettings
    {
        private string _domain = String.Empty;
        public string Domain
        {
            get { return _domain; }
            set { _domain = value; }
        }

        private string _tenantId = string.Empty;
        public string TenantID
        {
            get { return _tenantId; }
            set { _tenantId = value; }
        }

        private string _clientId { get; set; } = string.Empty;
        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        private bool _cachingToken { get; set; } = false;
        public bool CachingToken
        {
            get { return _cachingToken; }
            set { _cachingToken = value; }
        }

        private bool _isUpdated { get; set; } = false;
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set { _isUpdated = value; }
        }

        internal static string GetLocalAppPath()
        {
            Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string version;
            if (assemblyVersion != null)
            {
                version = assemblyVersion.ToString();
                String[] result = version.Split('.').ToArray();
                version = string.Join(".", result, 0, 3);
            }
            else
            {
                version = string.Empty;
            }
            string localAppPath = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NovaPoint", version);
            System.IO.Directory.CreateDirectory(localAppPath);

            return localAppPath;
        }

        private static string GetSettingsPath()
        {
            string settingsFile = Path.Combine(GetLocalAppPath(), "user.config");
            return settingsFile;
        }

        public static AppSettings GetSettings()
        {
            AppSettings appSettings;

            string settingsFile = GetSettingsPath();

            if (System.IO.File.Exists(settingsFile))
            {
                try
                {
                    XmlSerializer mySerializer = new XmlSerializer(typeof(AppSettings));
                    FileStream myFileStream = new(settingsFile, FileMode.Open);

                    appSettings = (AppSettings?)mySerializer.Deserialize(myFileStream);
                    myFileStream.Close();
                }
                catch
                {
                    appSettings = new();
                }

            }
            else
            {
                appSettings = new();
            }

            return appSettings;
        }

        public static void CheckForUpdates()
        {
            CheckForUpdatesAsync().
                ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task CheckForUpdatesAsync()
        {
            await Task.Run(async () =>
            {

                var appSettings = AppSettings.GetSettings();
                appSettings.IsUpdated = await VersionControl.IsUpdated();
                appSettings.SaveSettings();
            });
        }

        public void SaveSettings()
        {
            XmlSerializer mySerializer = new XmlSerializer(typeof(AppSettings));

            StreamWriter myWriter = new(new FileStream(GetSettingsPath(), FileMode.Create, FileAccess.Write));
            mySerializer.Serialize(myWriter, this);
            myWriter.Close();
        }

    }
}
