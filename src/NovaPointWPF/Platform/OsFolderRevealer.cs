using NovaPointLibrary.Core.Platform;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NovaPointWPF.Platform
{
    // NovaPointWPF only ever runs on Windows, but this keeps the per-OS branching T3.4 calls for,
    // so an Avalonia host can reuse this implementation as-is.
    public class OsFolderRevealer : IFolderRevealer
    {
        public void Reveal(string folderPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", folderPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", folderPath);
            else
                Process.Start("xdg-open", folderPath);
        }
    }
}
