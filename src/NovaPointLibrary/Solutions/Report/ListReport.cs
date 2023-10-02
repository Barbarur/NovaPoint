using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.List;
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

        private Main _main;

        public ListReport(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, ListReportParameters parameters)
        {
            Parameters = parameters;

            _main = new(this, appInfo, uiAddLog);
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
                else if (!_param.ListAll && String.IsNullOrWhiteSpace(_param.ListTitle))
                {
                    throw new Exception($"FORM INCOMPLETED: Library name cannot be empty when not processing all Libraries");
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _main.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _main.IsCancelled();

            SolutionProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                Web oSite = await new SPOSiteCSOM(_main).Get(_param.SiteUrl);

                progress = new(_main, 1);
                await ProcessSite(oSite.Url, progress);
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_main).Get(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_main, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    await ProcessSite(oSiteCollection.Url, progress);
                    progress.ProgressUpdateReport();
                }
            }

            _main.ScriptFinish();
        }

        private async Task ProcessSite(string siteUrl, SolutionProgressTracker progress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessSite";

            try
            {
                _main.AddLogToUI(methodName, $"Processing Site '{siteUrl}'");

                await new SPOSiteCollectionAdminCSOM(_main).Set(siteUrl, _param.AdminUPN);

                await ProcessLists(siteUrl, progress);

                await ProcessSubsites(siteUrl, progress);

                if (_param.RemoveAdmin)
                {
                    await new SPOSiteCollectionAdminCSOM(_main).Remove(siteUrl, _param.AdminUPN);
                }
            }
            catch (Exception ex)
            {
                _main.ReportError("Site", siteUrl, ex);

                AddRecord(siteUrl, remarks: ex.Message);
            }
        }

        private async Task ProcessSubsites(string siteUrl, SolutionProgressTracker progress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessSubsites";

            if (!_param.IncludeSubsites) { return; }

            var collSubsites = await new SPOSubsiteCSOM(_main).Get(siteUrl);

            progress.IncreaseTotalCount(collSubsites.Count);
            foreach (var oSubsite in collSubsites)
            {
                _main.AddLogToUI(methodName, $"Processing Subsite '{oSubsite.Title}'");

                try
                {
                    await ProcessLists(oSubsite.Url, progress);
                }
                catch (Exception ex)
                {
                    _main.ReportError("Subsite", oSubsite.Url, ex);

                    AddRecord(oSubsite.Url, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }

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

        private async Task ProcessLists(string siteUrl, SolutionProgressTracker parentPprogress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessLists";

            var collList = await new SPOListCSOM(_main).Get(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists, _listExpresions);

            SolutionProgressTracker progress = new(parentPprogress, collList.Count);
            foreach (var oList in collList)
            {
                AddRecord(siteUrl, oList);

                progress.ProgressUpdateReport();
            }
        }

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

            dynamicRecord.Rquest_ForceCheckout = list != null ? list.ForceCheckout.ToString() : string.Empty;
            
            dynamicRecord.Remarks = remarks;

            _main.AddRecordToCSV(dynamicRecord);
        }
    }

    public class ListReportParameters : ISolutionParameters
    {
        public string AdminUPN { get; set; } = String.Empty;
        public bool RemoveAdmin { get; set; } = false;

        public bool SiteAll { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool OnlyGroupIdDefined { get; set; } = false;
        public string SiteUrl { get; set; } = String.Empty;
        public bool IncludeSubsites { get; set; } = false;

        public bool ListAll { get; set; } = true;
        public bool IncludeHiddenLists { get; set; } = false;
        public bool IncludeSystemLists { get; set; } = false;
        public string ListTitle { get; set; } = String.Empty;
    }
}
