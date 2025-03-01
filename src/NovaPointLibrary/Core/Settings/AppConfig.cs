using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Authentication;


namespace NovaPointLibrary.Core.Settings
{
    public class AppConfig
    {
        private static readonly string _npLocalAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaPoint");

        public List<AppClientConfidentialProperties> ListConfidentialApps { get; set; } = new();
        public List<AppClientPublicProperties> ListPublicApps { get; set; } = new();

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
            ListPublicApps.Add(newPublicApp);
            return newPublicApp;
        }

        public void RemoveApp(AppClientPublicProperties app)
        {
            // TEST WHAT HAPPEN IF TRY TO REMOVE AN APP THAT IS NOT ON THE LIST.
            ListPublicApps.Remove(app);
            SaveSettings();
        }

        public void RemoveApp(AppClientConfidentialProperties app)
        {
            // TEST WHAT HAPPEN IF TRY TO REMOVE AN APP THAT IS NOT ON THE LIST.
            ListConfidentialApps.Remove(app);
            SaveSettings();
        }

        private void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetSettingsPath(), json);
        }

        public void SaveSettings(AppClientPublicProperties app)
        {
            app.ValidateProperties();
            if (!ListPublicApps.Contains(app))
            {
                ListPublicApps.Add(app);
            }
            SaveSettings();
        }

        public void SaveSettings(AppClientConfidentialProperties app)
        {
            app.ValidateProperties();
            if (!ListConfidentialApps.Contains(app))
            {
                ListConfidentialApps.Add(app);
            }
            SaveSettings();
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
