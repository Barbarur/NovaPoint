using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointWPF.Pages.Solutions
{
    public interface ISolutionForm
    {
        async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
        }
    }
}
