using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
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

        private ListReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ListReportParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
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

        public ListReport(ListReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _param.ListExpresions = _listExpresions;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param).GetListsAsync())
            {
                _appInfo.IsCancelled();

                AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);

            }

            //ProgressTracker progress;
            //if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            //{
            //    Web oSite = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(_param.SiteUrl);

            //    progress = new(_main, 1);
            //    await ProcessSite(oSite.Url, progress);
            //}
            //else
            //{
            //    List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_main).GetDeprecated(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

            //    progress = new(_main, collSiteCollections.Count);
            //    foreach (var oSiteCollection in collSiteCollections)
            //    {
            //        await ProcessSite(oSiteCollection.Url, progress);
            //        progress.ProgressUpdateReport();
            //    }
            //}
        }

        //private async Task ProcessSite(string siteUrl, ProgressTracker progress)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessSite";

        //    try
        //    {
        //        _main.AddLogToUI(methodName, $"Processing Site '{siteUrl}'");

        //        await new SPOSiteCollectionAdminCSOM(_main).SetDEPRECATED(siteUrl, _param.AdminUPN);

        //        await ProcessLists(siteUrl, progress);

        //        await ProcessSubsites(siteUrl, progress);

        //        if (_param.RemoveAdmin)
        //        {
        //            await new SPOSiteCollectionAdminCSOM(_main).RemoveDEPRECATED(siteUrl, _param.AdminUPN);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _main.ReportError("Site", siteUrl, ex);

        //        AddRecord(siteUrl, remarks: ex.Message);
        //    }
        //}

        //private async Task ProcessSubsites(string siteUrl, ProgressTracker progress)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessSubsites";

        //    if (!_param.IncludeSubsites) { return; }

        //    var collSubsites = await new SPOSubsiteCSOM(_main).GetDEPRECATED(siteUrl);

        //    progress.IncreaseTotalCount(collSubsites.Count);
        //    foreach (var oSubsite in collSubsites)
        //    {
        //        _main.AddLogToUI(methodName, $"Processing Subsite '{oSubsite.Title}'");

        //        try
        //        {
        //            await ProcessLists(oSubsite.Url, progress);
        //        }
        //        catch (Exception ex)
        //        {
        //            _main.ReportError("Subsite", oSubsite.Url, ex);

        //            AddRecord(oSubsite.Url, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        

        //private async Task ProcessLists(string siteUrl, ProgressTracker parentPprogress)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessLists";

        //    var collList = await new SPOListCSOM(_main).GetDEPRECATED(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists, _listExpresions);

        //    ProgressTracker progress = new(parentPprogress, collList.Count);
        //    foreach (var oList in collList)
        //    {
        //        AddRecord(siteUrl, oList);

        //        progress.ProgressUpdateReport();
        //    }
        //}

        private void AddRecord(string siteURL, Microsoft.SharePoint.Client.List? list = null, string remarks = "")
        {
            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.SiteURL = siteURL;

            dynamicRecord.ListType = list != null ? list.BaseType.ToString() : string.Empty;
            dynamicRecord.ListDefaultViewUrl = list != null ? list.DefaultViewUrl : string.Empty;
            dynamicRecord.ListID = list != null ? list.Id.ToString() : string.Empty;

            dynamicRecord.Created = list != null ? list.Created.ToString() : string.Empty;
            dynamicRecord.LastModifiedDate = list != null ? list.LastItemUserModifiedDate.ToString() : string.Empty;

            dynamicRecord.ItemCount = list != null ? list.ItemCount.ToString() : string.Empty;

            dynamicRecord.MajorVersioning = list != null ? list.EnableVersioning.ToString() : string.Empty;
            dynamicRecord.MajorVersionLimit = list != null ? list.MajorVersionLimit.ToString() : string.Empty;
            dynamicRecord.MinorVersioning = list != null ? list.EnableMinorVersions.ToString() : string.Empty; // might be a problem for Lists
            dynamicRecord.MinorVersionLimit = list != null ? list.MajorWithMinorVersionsLimit.ToString() : string.Empty;

            dynamicRecord.IRM_Emabled = list != null ? list.IrmEnabled.ToString() : string.Empty;

            dynamicRecord.ForceCheckout = list != null ? list.ForceCheckout.ToString() : string.Empty;
            
            dynamicRecord.Remarks = remarks;

            _logger.RecordCSV(dynamicRecord);
        }
    }

    public class ListReportParameters : SPOTenantListsParameters
    {

    }
}
