using Newtonsoft.Json;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Globalization;


namespace NovaPointLibrary.Core.Settings
{
    public class AppConfig
    {
        private static readonly string s_configFilePath = Path.Combine(AppFolders.GetConfigFolder(), "user.config");
        private static readonly string? s_configDir = Path.GetDirectoryName(s_configFilePath);
        
        public List<AppClientConfidentialProperties> ListAppClientConfidentialProperties { get; set; } = [];
        public List<AppClientPublicProperties> ListAppClientPublicProperties { get; set; } = [];

        internal AppConfig() { }

        public static AppConfig GetSettings()
        {
            AppConfig appSettings;

            if (File.Exists(s_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(s_configFilePath);
                    appSettings = JsonConvert.DeserializeObject<AppConfig>(json) ?? throw new InvalidOperationException("Failed to deserialize JSON.");
                }
                catch (Exception ex)
                {
                    // The file exists but couldn't be read/parsed. Preserve the original and record why.
                    BackupCorruptSettings(s_configFilePath, ex);
                    appSettings = new();
                }

            }
            else
            {
                appSettings = new();
            }

            return appSettings;
        }

        private static void BackupCorruptSettings(string configFile, Exception ex)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);
                string backupFile = $"{configFile}.corrupt-{timestamp}";
                File.Copy(configFile, backupFile, overwrite: true);

                LogCrash.WriteCrashLog(ex, "Config");
            }
            catch
            {
                // Best-effort logging only; a failure here should not throw again.
            }
        }

        public IAppClientProperties GetOriginalSettings(IAppClientProperties clientProperties)
        {
            if (clientProperties is AppClientConfidentialProperties confidentialProperties)
            {
                return ListAppClientConfidentialProperties.Find(p => p.Id == confidentialProperties.Id)
                    ?? throw new InvalidOperationException($"Confidential app (Id '{confidentialProperties.Id}', ClientId '{confidentialProperties.ClientId}') does not exist in settings.");
            }

            else if (clientProperties is AppClientPublicProperties publicProperties)
            {
                return ListAppClientPublicProperties.Find(p => p.Id == publicProperties.Id)
                    ?? throw new InvalidOperationException($"Public app (Id '{publicProperties.Id}', ClientId '{publicProperties.ClientId}') does not exist in settings.");
            }
            throw new ArgumentException("App properties is neither public nor confidential.", nameof(clientProperties));
        }

        public async Task RemoveApp(IAppClientProperties clientProperties)
        {
            if (clientProperties is AppClientConfidentialProperties confidentialProperties)
            {
                ListAppClientConfidentialProperties.RemoveAll(p => p.Id == confidentialProperties.Id);
            }
            else if (clientProperties is AppClientPublicProperties publicProperties)
            {
                ListAppClientPublicProperties.RemoveAll(p => p.Id == publicProperties.Id);

                // Clear the token cache for the removed app, unless another saved app still uses the same ClientId.
                bool clientIdStillInUse = ListAppClientPublicProperties.Any(p => p.ClientId == publicProperties.ClientId);
                if (!clientIdStillInUse)
                {
                    await TokenCacheHelper.RemoveCache(new[] { publicProperties.ClientId });
                }
            }
            SaveSettings();
        }

        public async Task SaveSettings(IAppClientProperties clientProperties)
        {
            clientProperties.ValidateProperties();

            if (clientProperties is AppClientConfidentialProperties confidentialProperties)
            {
                int index = ListAppClientConfidentialProperties.FindIndex(p => p.Id == confidentialProperties.Id);
                if (index != -1) { ListAppClientConfidentialProperties[index] = confidentialProperties.Clone(); }
                else { ListAppClientConfidentialProperties.Add(confidentialProperties); }
            }

            else if (clientProperties is AppClientPublicProperties publicProperties)
            {
                int index = ListAppClientPublicProperties.FindIndex(p => p.Id == publicProperties.Id);
                if (index != -1) { ListAppClientPublicProperties[index] = publicProperties.Clone(); }
                else { ListAppClientPublicProperties.Add(publicProperties); }

                if (!publicProperties.CachingToken)
                {
                    await TokenCacheHelper.RemoveCache(new[] { publicProperties.ClientId });
                }
            }

            SaveSettings();
        }

        private void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);

            // Ensure folder exists.
            System.IO.Directory.CreateDirectory(s_configDir!);

            // Write to a sibling temp file, then atomically replace the real file.
            string settingsFile = s_configFilePath;
            string tempFile = settingsFile + ".tmp";

            File.WriteAllText(tempFile, json);
            File.Move(tempFile, settingsFile, overwrite: true);
        }

        public static async Task RemoveTokenCache()
        {
            var clientIds = GetSettings().ListAppClientPublicProperties.Select(p => p.ClientId);
            await TokenCacheHelper.RemoveCache(clientIds);
        }

    }
}
