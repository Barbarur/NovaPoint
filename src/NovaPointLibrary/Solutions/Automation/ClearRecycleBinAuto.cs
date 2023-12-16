using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class ClearRecycleBinAuto : ISolution
    {
        public static readonly string s_SolutionName = "Delete items from recycle bin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ClearRecycleBinAuto";

        private ClearRecycleBinAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ClearRecycleBinAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public ClearRecycleBinAuto(AppInfo appInfo, Action<LogInfo> uiAddLog, ClearRecycleBinAutoParameters parameters)
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

            await new SPOTenantRecycleBinItem(_logger, _appInfo, _param.GetRecycleBinParameters()).ClearAsync();            

            _logger.ScriptFinish();
        }
    }

    public class ClearRecycleBinAutoParameters : SPORecycleBinItemParameters, ISolutionParameters
    {
        internal SPORecycleBinItemParameters GetRecycleBinParameters()
        {
            return this;
        }
    }
}
