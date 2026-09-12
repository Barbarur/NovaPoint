using NovaPointLibrary.Core.Platform;
using System.Diagnostics;

namespace NovaPointWPF.Platform
{
    // UseShellExecute hands the URL to the OS shell as an argument, not a command line, which
    // closes the "cmd /c start {url}" shell-injection seam the call sites use today.
    public class ShellUrlLauncher : IUrlLauncher
    {
        public void Open(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
