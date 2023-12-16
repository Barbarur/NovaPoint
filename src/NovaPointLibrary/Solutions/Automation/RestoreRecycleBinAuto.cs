using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RestoreRecycleBinAuto : ISolution
    {
        public static readonly string s_SolutionName = "Restore items from recycle bin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-RestoreRecycleBinAuto";

        private RestoreRecycleBinAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RestoreRecycleBinAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public RestoreRecycleBinAuto(AppInfo appInfo, Action<LogInfo> uiAddLog, RestoreRecycleBinAutoParameters parameters)
        {
            Parameters = parameters;
            _appInfo = appInfo;
            _logger = new(uiAddLog, this);
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(_param.AdminUPN))
                {
                    throw new Exception("FORM INCOMPLETED: Admin UPN cannot be empty.");
                }
                else if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
                {
                    throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await new SPOTenantRecycleBinItem(_logger, _appInfo, _param.GetRecycleBinParameters()).RestoreAsync();

            _logger.ScriptFinish();
        }
    }

    public class RestoreRecycleBinAutoParameters : SPORecycleBinItemParameters, ISolutionParameters
    {
        internal SPORecycleBinItemParameters GetRecycleBinParameters()
        {
            return this;
        }
    }
}
