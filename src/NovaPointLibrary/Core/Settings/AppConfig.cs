using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Authentication;


namespace NovaPointLibrary.Core.Settings
{
    public class AppConfig
    {
        private static readonly string _npLocalAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaPoint");

        public List<AppClientConfidentialProperties> ListAppClientConfidentialProperties { get; set; } = [];
        public List<AppClientPublicProperties> ListAppClientPublicProperties { get; set; } = [];

        internal AppConfig() { }

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

        public static AppConfig GetSettings()
        {
            AppConfig appSettings;

            AppConfig.RemoveLegacyData();

            string settingsFile = GetSettingsPath();

            if (File.Exists(settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(settingsFile);
                    appSettings = JsonConvert.DeserializeObject<AppConfig>(json) ?? throw new InvalidOperationException("Failed to deserialize JSON.");
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

        public AppClientPublicProperties GetNewPublicApp()
        {
            AppClientPublicProperties newPublicApp = new();
            ListAppClientPublicProperties.Add(newPublicApp);
            return newPublicApp;
        }

        public void RemoveApp(IAppClientProperties clientProperties)
        {
            if (clientProperties is AppClientConfidentialProperties confidentialProperties) { ListAppClientConfidentialProperties.Remove(confidentialProperties); }
            else if (clientProperties is AppClientPublicProperties publicProperties) { ListAppClientPublicProperties.Remove(publicProperties); }
        }

        public void SaveSettings(IAppClientProperties clientProperties)
        {
            clientProperties.ValidateProperties();
            if (clientProperties is AppClientConfidentialProperties confidentialProperties && !ListAppClientConfidentialProperties.Contains(confidentialProperties)) { ListAppClientConfidentialProperties.Add(confidentialProperties); }
            else if (clientProperties is AppClientPublicProperties publicProperties && !ListAppClientPublicProperties.Contains(publicProperties)) { ListAppClientPublicProperties.Add(publicProperties); }
            SaveSettings();
        }

        private void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetSettingsPath(), json);
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
