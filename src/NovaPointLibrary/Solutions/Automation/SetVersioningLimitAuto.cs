using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetVersioningLimitAuto : ISolution
    {
        public static readonly string s_SolutionName = "Set versioning limit";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-SetVersioningLimitAuto";

        private SetVersioningLimitAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {

            l => l.Hidden,

            l => l.BaseType,
            l => l.Title,
            l => l.DefaultViewUrl,
            l => l.Id,

            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.EnableMinorVersions,
            l => l.MajorWithMinorVersionsLimit,
        };

        public SetVersioningLimitAuto(SetVersioningLimitAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            _param = parameters;
            _param.TListsParam.ListParam.ListExpresions = _listExpresions;
            _logger = new(uiAddLog, this.GetType().Name, _param);
            _appInfo = new(_logger, cancelTokenSource);
        }

        //public SetVersioningLimitAuto(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, SetVersioningLimitAutoParameters parameters)
        //{
        //    Parameters = parameters;

        //    _main = new(this, appInfo, uiAddLog);
        //}

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

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);
                    continue;
                }

                try
                {
                    await SetVersioning(results.SiteUrl, results.List);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
                    AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                }
            }

            //ProgressTracker progress;
            //if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            //{
            //    Web oSite = await new SPOSiteCSOM(_main).GetToDeprecate(_param.SiteUrl);

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

            //_main.ScriptFinish();
        }

        //private async Task ProcessSite(string siteUrl, ProgressTracker progress)
        //{
        //    _main.IsCancelled();
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

        //        AddRecord(siteUrl, string.Empty, remarks: ex.Message);
        //    }
        //}

        //private async Task ProcessSubsites(string siteUrl, ProgressTracker progress)
        //{
        //    _main.IsCancelled();
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

        //            AddRecord(oSubsite.Url, string.Empty, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private async Task ProcessLists(string siteUrl, ProgressTracker parentPprogress)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessLists";

        //    var collList = await new SPOListCSOM(_main).GetDEPRECATED(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);

        //    ProgressTracker progress = new(parentPprogress, collList.Count);
        //    foreach (var oList in collList)
        //    {
        //        _main.AddLogToUI(methodName, $"Processing '{oList.BaseType}' - '{oList.Title}'");

        //        try
        //        {
        //            await SetVersioning(siteUrl, oList.Title);

        //            AddRecord(siteUrl, oList.Title, "");
        //        }
        //        catch (Exception ex)
        //        {
        //            _main.ReportError("Subsite", siteUrl, ex);

        //            AddRecord(siteUrl, oList.Title, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //{

        //    l => l.Hidden,

        //    l => l.BaseType,
        //    l => l.Title,
        //    l => l.DefaultViewUrl,
        //    l => l.Id,

        //    l => l.EnableVersioning,
        //    l => l.MajorVersionLimit,
        //    l => l.EnableMinorVersions,
        //    l => l.MajorWithMinorVersionsLimit,
        //};

        private async Task SetVersioning(string siteUrl, Microsoft.SharePoint.Client.List olist)
        {
            _appInfo.IsCancelled();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.GetListByTitle(olist.Title, _listExpresions);

            int majorVersions = 0;
            int minorVersions = 0;

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                majorVersions = _param.LibraryMajorVersionLimit;
                minorVersions = _param.LibraryMinorVersionLimit;

            }
            else if (oList.BaseType == BaseType.GenericList)
            {
                majorVersions = _param.ListMajorVersionLimit;
                minorVersions = 0;
            }

            bool enableVersioning = majorVersions > 0;
            bool enableMinorVersions = minorVersions > 0;
            bool updateRequired = false;

            if (enableVersioning != oList.EnableVersioning)
            {
                oList.EnableVersioning = enableVersioning;
                updateRequired = true;
            }

            if (enableVersioning)
            {
                oList.MajorVersionLimit = majorVersions;
                updateRequired = true;
            }

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                if (enableVersioning && enableMinorVersions != oList.EnableMinorVersions)
                {
                    oList.EnableMinorVersions = enableMinorVersions;
                    updateRequired = true;
                }

                if (enableVersioning && enableMinorVersions)
                {
                    oList.MajorWithMinorVersionsLimit = (int)minorVersions;
                    updateRequired = true;
                }
            }

            if (updateRequired)
            {
                _logger.LogTxt(GetType().Name, $"Updating '{oList.BaseType}' - '{oList.Title}'");
                oList.Update();
                clientContext.ExecuteQuery();
            }

        }

        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               string remarks = "")
        {
            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.SiteUrl = siteUrl;
            dynamicRecord.ListTitle = oList != null ? oList.Title : String.Empty;
            dynamicRecord.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            dynamicRecord.Remarks = remarks;

            _logger.DynamicCSV(dynamicRecord);
        }




    }

    public class SetVersioningLimitAutoParameters : ISolutionParameters
    {
        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;

        public SPOTenantListsParameters TListsParam {  get; set; }

        public SetVersioningLimitAutoParameters(SPOTenantListsParameters listsParameters)
        {
            TListsParam = listsParameters;
        }

        internal void ParametersCheck()
        {
            if (LibraryMajorVersionLimit < 1 && LibraryMinorVersionLimit > 0)
            {
                throw new Exception($"FORM INCOMPLETED: You cannot set Minor verion limit for a library without setting Major version limit.");
            }
        }
    }
}
