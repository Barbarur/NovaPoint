using NovaPointLibrary.Commands.Utilities;
using System.Xml.Serialization;

namespace NovaPointLibrary.Commands.Authentication
{
    public class AppSettings
    {
        private string _tenantId = string.Empty;
        public string TenantID
        {
            get { return _tenantId; }
            set { _tenantId = value.Trim(); }
        }

        private string _clientId { get; set; } = string.Empty;
        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value.Trim(); }
        }

        private bool _cachingToken { get; set; } = false;
        public bool CachingToken
        {
            get { return _cachingToken; }
            set { _cachingToken = value; }
        }

        private bool _isUpdated { get; set; } = true;
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set { _isUpdated = value; }
        }

        private static readonly string _npLocalAppFolder = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NovaPoint");

        internal static string GetLocalAppPath()
        {
            string localAppPath = Path.Combine(_npLocalAppFolder, VersionControl.GetVersion());
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

            AppSettings.RemoveLegacyData();

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

        public static async Task CheckForUpdatesAsync()
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

        public void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(TenantID) || string.IsNullOrWhiteSpace(ClientId))
            {
                throw new Exception("Please go to Settings and fill the App Information");
            }
        }

        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }

        private static void RemoveLegacyData()
        {
            string localAppPathFolderData = GetLocalAppPath();

            string[] localAppPathFolders = System.IO.Directory.GetDirectories(_npLocalAppFolder);
            foreach (var folderPath in localAppPathFolders)
            {
                if (!String.Equals(localAppPathFolderData, folderPath) && System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.Delete(folderPath, recursive: true);
                }
            }
        }

    }
}
