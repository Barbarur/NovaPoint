namespace NovaPointLibrary.Core.Settings
{
    public static class AppFolders
    {
        private const string AppName = "NovaPoint";

        // Windows: %LOCALAPPDATA%\NovaPoint
        // macOS/Linux: ~/.local/share/NovaPoint
        public static string GetConfigFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);
        }

        // Windows: %LOCALAPPDATA%\NovaPoint
        // macOS/Linux: $XDG_CACHE_HOME/NovaPoint  (falls back to ~/.cache/NovaPoint)
        public static string GetCacheFolder()
        {
            string baseCache;
            if (OperatingSystem.IsWindows())
            {
                baseCache = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else
            {
                baseCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
                            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            }
            return Path.Combine(baseCache, AppName);
        }

        // Windows/macOS: ~/Documents/NovaPoint
        // Linux: ~/Documents/NovaPoint  (MyDocuments returns $HOME on Unix — we append Documents manually)
        public static string GetOutputFolder()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(docs) ||
                docs == Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            {
                docs = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Documents");
            }
            return Path.Combine(docs, AppName);
        }
    }
}
