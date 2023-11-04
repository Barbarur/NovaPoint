using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
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

        private SetVersioningLimitAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (SetVersioningLimitAutoParameters)value; }
        }

        private Main _main;

        public SetVersioningLimitAuto(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, SetVersioningLimitAutoParameters parameters)
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
                else if (_param.LibraryMajorVersionLimit < 1 && _param.LibraryMinorVersionLimit > 0)
                {
                    throw new Exception($"FORM INCOMPLETED: You cannot set Minor verion limit for a library without setting Major version limit.");
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

            ProgressTracker progress;
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

        private async Task ProcessSite(string siteUrl, ProgressTracker progress)
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

                AddRecord(siteUrl, string.Empty, remarks: ex.Message);
            }
        }

        private async Task ProcessSubsites(string siteUrl, ProgressTracker progress)
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

                    AddRecord(oSubsite.Url, string.Empty, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }

        private async Task ProcessLists(string siteUrl, ProgressTracker parentPprogress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessLists";

            var collList = await new SPOListCSOM(_main).Get(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);

            ProgressTracker progress = new(parentPprogress, collList.Count);
            foreach (var oList in collList)
            {
                _main.AddLogToUI(methodName, $"Processing '{oList.BaseType}' - '{oList.Title}'");

                try
                {
                    await SetVersioning(siteUrl, oList.Title);

                    AddRecord(siteUrl, oList.Title, "");
                }
                catch (Exception ex)
                {
                    _main.ReportError("Subsite", siteUrl, ex);

                    AddRecord(siteUrl, oList.Title, remarks: ex.Message);
                }

                //int majorVersions = 0;
                //int minorVersions = 0;

                //if (oList.BaseType == BaseType.DocumentLibrary)
                //{
                //    majorVersions = _param.LibraryMajorVersionLimit;
                //    minorVersions = _param.LibraryMinorVersionLimit;

                //}
                //else if (oList.BaseType == BaseType.GenericList)
                //{
                //    majorVersions = _param.ListMajorVersionLimit;
                //    minorVersions = 0;
                //}

                //bool enableVersioning = majorVersions > 0;
                //bool enableMinorVersions = minorVersions > 0;
                //bool updateRequired = false;

                //if (enableVersioning != oList.EnableVersioning)
                //{
                //    oList.EnableVersioning = enableVersioning;
                //    updateRequired = true;
                //}

                //if (enableVersioning)
                //{
                //    oList.MajorVersionLimit = majorVersions;
                //    updateRequired = true;
                //}

                //if (oList.BaseType == BaseType.DocumentLibrary)
                //{
                //    if (enableVersioning && enableMinorVersions != oList.EnableMinorVersions)
                //    {
                //        oList.EnableMinorVersions = enableMinorVersions;
                //        updateRequired = true;
                //    }

                //    if (enableVersioning && enableMinorVersions)
                //    {
                //        oList.MajorWithMinorVersionsLimit = (int)minorVersions;
                //        updateRequired = true;
                //    }
                //}

                //if (updateRequired)
                //{
                //    oList.Update();
                //    clientContext.ExecuteQuery();
                //}



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

            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.EnableMinorVersions,
            l => l.MajorWithMinorVersionsLimit,
        };

        private async Task SetVersioning(string siteUrl, string listTitle)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.SetVersioning";

            ClientContext clientContext = await _main.GetContext(siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.GetListByTitle(listTitle, _listExpresions);

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
                _main.AddLogToTxt(methodName, $"Updating '{oList.BaseType}' - '{oList.Title}'");
                oList.Update();
                clientContext.ExecuteQuery();
            }

        }

        //private void GetLists(string siteURL, ClientContext clientContext, ProgressTracker parentProgress)
        //{
        //    _appInfo.IsCancelled();

        //    Expression<Func<Microsoft.SharePoint.Client.List, object>>[] listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //    {
        //    l => l.Hidden,

        //    l => l.BaseType,
        //    l => l.Title,
        //    l => l.DefaultViewUrl,
        //    l => l.Id,

        //    l => l.EnableVersioning,
        //    l => l.MajorVersionLimit,
        //    l => l.EnableMinorVersions,
        //    l => l.MajorWithMinorVersionsLimit,
        //    };

        //    List<Microsoft.SharePoint.Client.List> collList = new SPOListCSOM(_logHelper, _appInfo, clientContext, _param.ListName, _param.IncludeSystemLists, _param.IncludeResourceLists).Get(listExpresions);
        //    ProgressTracker progress = new(parentProgress, collList.Count);
        //    foreach (Microsoft.SharePoint.Client.List oList in collList)
        //    {
        //        int majorVersions = 0;
        //        int minorVersions = 0;

        //        if (oList.BaseType == BaseType.DocumentLibrary)
        //        {
        //            majorVersions = _param.LibraryMajorVersionLimit;
        //            minorVersions = _param.LibraryMinorVersionLimit;

        //        }
        //        else if (oList.BaseType == BaseType.GenericList)
        //        {
        //            majorVersions = _param.ListMajorVersionLimit;
        //            minorVersions = 0;
        //        }

        //        bool enableVersioning = majorVersions > 0;
        //        bool enableMinorVersions = minorVersions > 0;
        //        bool updateRequired = false;

        //        if (enableVersioning != oList.EnableVersioning)
        //        {
        //            //_logHelper.AddLogToTxt(methodName, $"Enable MajorVersions");
        //            oList.EnableVersioning = enableVersioning;
        //            updateRequired = true;
        //        }

        //        if (enableVersioning)
        //        {
        //            //_logHelper.AddLogToTxt(methodName, $"MajorVersions '{majorVersions}'");
        //            oList.MajorVersionLimit = majorVersions; // Check if this property can be called for Lists without crashing
        //            updateRequired = true;
        //        }

        //        if (oList.BaseType == BaseType.DocumentLibrary)
        //        {
        //            if (enableVersioning && enableMinorVersions != oList.EnableMinorVersions)
        //            {
        //                //_logHelper.AddLogToTxt(methodName, $"Enable MinorVersions");
        //                oList.EnableMinorVersions = enableMinorVersions;
        //                updateRequired = true;
        //            }

        //            if (enableVersioning && enableMinorVersions)
        //            {
        //                //_logHelper.AddLogToTxt(methodName, $"MinorVersions '{minorVersions}'");
        //                oList.MajorWithMinorVersionsLimit = (int)minorVersions;
        //                updateRequired = true;
        //            }
        //        }

        //        if (updateRequired)
        //        {
        //            //_logHelper.AddLogToTxt(methodName, $"Update Required");
        //            oList.Update();
        //            clientContext.ExecuteQuery();
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        private void AddRecord(string siteUrl, string listName, string remarks)
        {
            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.SiteUrl = siteUrl;
            dynamicRecord.ListName = listName;
            dynamicRecord.Remarks = remarks;

            _main.AddRecordToCSV(dynamicRecord);
        }




    }

    public class SetVersioningLimitAutoParameters : ISolutionParameters
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

        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;

    }
}
