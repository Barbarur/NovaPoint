using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class RecycleBinReport : ISolution
    {
        public static readonly string s_SolutionName = "Recycle bin report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-RecycleBinReport";

        private RecycleBinReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RecycleBinReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public RecycleBinReport(AppInfo appInfo, Action<LogInfo> uiAddLog, RecycleBinReportParameters parameters)
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
                    _logger.ScriptFinish();
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

            await new SPOTenantRecycleBinItem(_logger, _appInfo, _param.GetRecycleBinParameters()).ReportAsync();

        }

    }

    public class RecycleBinReportParameters : SPORecycleBinItemParameters, ISolutionParameters
    {
        internal SPORecycleBinItemParameters GetRecycleBinParameters()
        {
            return this;
        }
    }
}
