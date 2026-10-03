using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Settings;
using System.Globalization;

namespace NovaPointLibrary.Core.Logging;

public static class LogCrash
{
    // Kept together so writing and parsing the version cannot drift apart.
    private const string VersionTag = "_v";
    private const string CrashLogExtension = ".Log";

    private static readonly string s_crashFolder = Path.Combine(AppFolders.GetOutputFolder(), "CrashReport");

    public static string WriteCrashLog(Exception ex, string crashName)
    {
        string version = VersionControl.GetVersion();
        string logFile = Path.Combine(s_crashFolder, string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:yyMMddHHmmss}_{crashName}{VersionTag}{version}{CrashLogExtension}"));

        try
        {
            Directory.CreateDirectory(s_crashFolder);
            File.AppendAllText(logFile, string.Create(CultureInfo.InvariantCulture, $"NovaPointLibrary v{version}{Environment.NewLine}{DateTime.Now:yyyy-MM-dd HH:mm:ss} {ex}{Environment.NewLine}{Environment.NewLine}"));
        }
        catch
        {
            // Best-effort logging only; a failure here should not throw again.
        }

        return logFile;
    }

    // A stack trace is only meaningful for the build that produced it, so crash logs left behind by an
    // earlier version (or written before logs carried a version) are dropped.
    public static void RemoveLegacyCrashLogs()
    {
        if (!Directory.Exists(s_crashFolder)) { return; }

        string currentVersion = VersionControl.GetVersion();

        // Without a version to compare against every log would look legacy; leave them all alone instead.
        if (string.IsNullOrEmpty(currentVersion)) { return; }

        foreach (var filePath in Directory.GetFiles(s_crashFolder))
        {
            // Filtering here rather than with a GetFiles search pattern; the pattern is case-insensitive on
            // Windows but case-sensitive on Linux, and the behavior must be the same on every OS.
            if (!filePath.EndsWith(CrashLogExtension, StringComparison.OrdinalIgnoreCase)) { continue; }

            if (String.Equals(GetVersionFromFileName(filePath), currentVersion, StringComparison.OrdinalIgnoreCase)) { continue; }

            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // A single locked or unreadable file should not abort the clean up.
            }
        }
    }

    private static string GetVersionFromFileName(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        // LastIndexOf so a crash name that itself contains the tag does not get parsed as the version.
        int tagIndex = fileName.LastIndexOf(VersionTag, StringComparison.Ordinal);

        return tagIndex < 0 ? string.Empty : fileName[(tagIndex + VersionTag.Length)..];
    }

}
