using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Report
{
    public class ListReport : ISolution
    {
        public static readonly string s_SolutionName = "Libraries and Lists report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ListReport";

        private ListReportParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.Hidden,

            l => l.BaseType,
            l => l.Title,
            l => l.DefaultViewUrl,
            l => l.Id,

            l => l.Created,
            l => l.LastItemUserModifiedDate,

            l => l.ItemCount,

            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.EnableMinorVersions,
            l => l.MajorWithMinorVersionsLimit,

            l => l.IrmEnabled,

            l => l.ForceCheckout,
        };

        private ListReport(NPLogger logger, Commands.Authentication.AppInfo appInfo, ListReportParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(ListReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.TListsParam.ListParam.ListExpresions = _listExpresions;

            NPLogger logger = new(uiAddLog, "ListReport", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new ListReport(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        //public ListReport(ListReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{
        //    Parameters = parameters;
        //    _param.TListsParam.ListParam.ListExpresions = _listExpresions;
        //    _logger = new(uiAddLog, this.GetType().Name, parameters);
        //    _appInfo = new(_logger, cancelTokenSource);
        //}

        //public async Task RunAsync()
        //{
        //    try
        //    {
        //        await RunScriptAsync();

        //        _logger.ScriptFinish();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ScriptFinish(ex);
        //    }
        //}

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);

            }
        }

        private void AddRecord(string siteURL, Microsoft.SharePoint.Client.List? oList = null, string remarks = "")
        {
            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.SiteURL = siteURL;

            dynamicRecord.ListTitle = oList != null ? oList.Title : String.Empty;
            dynamicRecord.ListType = oList != null ? oList.BaseType.ToString() : string.Empty;
            dynamicRecord.ListDefaultViewUrl = oList != null ? oList.DefaultViewUrl : string.Empty;
            dynamicRecord.ListID = oList != null ? oList.Id.ToString() : string.Empty;

            dynamicRecord.Hidden = oList != null ? oList.Hidden.ToString() : string.Empty;
            dynamicRecord.IsSystemList = oList != null ? oList.IsSystemList.ToString() : string.Empty;

            dynamicRecord.Created = oList != null ? oList.Created.ToString() : string.Empty;
            dynamicRecord.LastModifiedDate = oList != null ? oList.LastItemUserModifiedDate.ToString() : string.Empty;

            dynamicRecord.ItemCount = oList != null ? oList.ItemCount.ToString() : string.Empty;

            dynamicRecord.MajorVersioning = oList != null ? oList.EnableVersioning.ToString() : string.Empty;
            dynamicRecord.MajorVersionLimit = oList != null ? oList.MajorVersionLimit.ToString() : string.Empty;
            dynamicRecord.MinorVersioning = oList != null ? oList.EnableMinorVersions.ToString() : string.Empty; // might be a problem for Lists
            dynamicRecord.MinorVersionLimit = oList != null ? oList.MajorWithMinorVersionsLimit.ToString() : string.Empty;

            dynamicRecord.IRM_Emabled = oList != null ? oList.IrmEnabled.ToString() : string.Empty;

            dynamicRecord.ForceCheckout = oList != null ? oList.ForceCheckout.ToString() : string.Empty;
            
            dynamicRecord.Remarks = remarks;

            _logger.DynamicCSV(dynamicRecord);
        }
    }

    public class ListReportParameters : ISolutionParameters
    {
        public SPOTenantListsParameters TListsParam {  get; set; }
        public ListReportParameters(SPOTenantListsParameters tenantListsParam)
        {
            TListsParam = tenantListsParam;
        }
    }
}
