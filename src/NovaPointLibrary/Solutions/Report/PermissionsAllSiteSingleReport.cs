//using Microsoft.Online.SharePoint.TenantAdministration;
//using Microsoft.SharePoint.Client;
//using NovaPointLibrary.Commands.Authentication;
//using NovaPointLibrary.Commands.SharePoint.Item;
//using NovaPointLibrary.Commands.SharePoint.List;
//using NovaPointLibrary.Commands.SharePoint.Permision;
//using NovaPointLibrary.Commands.SharePoint.Site;
//using NovaPointLibrary.Commands.Site;
//using NovaPointLibrary.Solutions.Automation;
//using System.Diagnostics.Metrics;
//using System.Dynamic;

//namespace NovaPointLibrary.Solutions.Report
//{
//    public class PermissionsAllSiteSingleReport
//    {
//        public static string _solutionName = "Permissions in a Site report";
//        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PermissionAllSiteSingleReport";

//        private PermissionsAllSiteSingleParameters _param = new();
//        public ISolutionParameters Parameters
//        {
//            get { return _param; }
//            set { _param = (PermissionsAllSiteSingleParameters)value; }
//        }

//        private readonly NPLogger _logger;
//        private readonly Commands.Authentication.AppInfo _appInfo;

//        public PermissionsAllSiteSingleReport(PermissionsAllSiteSingleParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
//        {
//            Parameters = parameters;
//            _logger = new(uiAddLog, this.GetType().Name, parameters);
//            _appInfo = new(_logger, cancelTokenSource);

//            SiteUrl = parameters.SiteUrl;

//            IncludeAdmins = parameters.IncludeAdmins;
//            IncludeSiteAccess = parameters.IncludeSiteAccess;
//            IncludeUniquePermissions = parameters.IncludeUniquePermissions;
//            IncludeSubsites = parameters.IncludeSubsites;

//            IncludeSystemLists = parameters.IncludeSystemLists;
//            IncludeResourceLists = parameters.IncludeResourceLists;
//        }

//        //private readonly NPLogger _logger;
//        //private readonly Commands.Authentication.AppInfo _appInfo;

//        private readonly string SiteUrl;
        
//        private readonly bool IncludeAdmins;
//        private readonly bool IncludeSiteAccess;
//        private readonly bool IncludeUniquePermissions;
//        private readonly bool IncludeSubsites;

//        private readonly bool IncludeSystemLists;
//        private readonly bool IncludeResourceLists;

//        List<SPORoleAssignmentKnownGroup> KnownGroups { get; set; } = new() { };

//        //public PermissionsAllSiteSingleReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, PermissionsAllSiteSingleParameters parameters)
//        //{
//        //    _logger = new(uiAddLog, "Reports", GetType().Name);
//        //    _appInfo = appInfo;

//        //    SiteUrl = parameters.SiteUrl;

//        //    IncludeAdmins = parameters.IncludeAdmins;
//        //    IncludeSiteAccess = parameters.IncludeSiteAccess;
//        //    IncludeUniquePermissions = parameters.IncludeUniquePermissions;
//        //    IncludeSubsites = parameters.IncludeSubsites;

//        //    IncludeSystemLists = parameters.IncludeSystemLists;
//        //    IncludeResourceLists = parameters.IncludeResourceLists;
//        //}

//        public async Task RunAsync()
//        {
//            try
//            {
//                if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
//                {
//                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Lists/Libraries";
//                    Exception ex = new(message);
//                    throw ex;
//                }
//                else if (IncludeAdmins == false && IncludeSiteAccess == false && IncludeUniquePermissions == false)
//                {
//                    string message = "FORM INCOMPLETED: YOu are not requesting any permissions. Report is Empty";
//                    Exception ex = new(message);
//                    throw ex;
//                }
//                else
//                {
//                    await RunScriptAsyncNEW();
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.ScriptFinish(ex);
//            }
//        }

//        private async Task RunScriptAsyncNEW()
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.SingleSiteAsync";


//            string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
//            string spoSiteAccessToken = await _appInfo.GetSPOAccessToken(rootUrl);
//            //string spoSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(rootUrl);
//            string aadAccessToken = await _appInfo.GetGraphAccessToken();
//            //string aadAccessToken = await new GetAccessToken(_logger, _appInfo).GraphInteractiveAsync();

//            GetSPOSitePermissions getPermissions = new(_logger, _appInfo, spoSiteAccessToken, aadAccessToken, KnownGroups);

//            Web oSite = new GetSPOSite(_logger, _appInfo, spoSiteAccessToken).CSOMWithRoles(SiteUrl);

//            ProgressTracker progress = new(_logger, 1);
//            _logger.LogTxt(methodName, $"Processing Site '{oSite.Title}'");

//            AddRecordToCSV( await getPermissions.CSOMSiteAsync(oSite, IncludeAdmins, IncludeSiteAccess, IncludeUniquePermissions, IncludeSystemLists, IncludeResourceLists) );

//            if (IncludeSubsites)
//            {
//                var collSubsites = new GetSubsite(_logger, _appInfo, spoSiteAccessToken).CsomAllSubsitesWithRolesAndSiteDetails(SiteUrl);
//                ProgressTracker progressSubsite = new(progress, collSubsites.Count);
//                foreach (var oSubsite in collSubsites)
//                {
//                    _logger.LogTxt(methodName, $"Processing Subsite '{oSubsite.Title}'");

//                    AddRecordToCSV( await getPermissions.CSOMSubsiteAsync(oSubsite, IncludeSiteAccess, IncludeUniquePermissions, IncludeSystemLists, IncludeResourceLists) );

//                    progressSubsite.ProgressUpdateReport();
//                }
//            }
//            progress.ProgressUpdateReport();

//            _logger.ScriptFinish();
//        }

//        private void AddRecordToCSV(List<SPOLocationPermissionsListRecord> recordsList)
//        {
//            _appInfo.IsCancelled();
            
//            foreach (var record in recordsList)
//            {
//                foreach (var roleAssigmentUser in record.SPORoleAssignmentUsersList)
//                {
//                    dynamic dynamicRecord = new ExpandoObject();
//                    dynamicRecord.LocationType = record.LocationType;
//                    dynamicRecord.LocationName = record.LocationName;
//                    dynamicRecord.LocationUrl = record.LocationUrl;


//                    dynamicRecord.AccessType = roleAssigmentUser.AccessType;
//                    dynamicRecord.AccountType = roleAssigmentUser.AccountType;
//                    dynamicRecord.Users = roleAssigmentUser.Users;
//                    dynamicRecord.PermissionLevels = roleAssigmentUser.PermissionLevels;

//                    dynamicRecord.Remarks = roleAssigmentUser.Remarks;

//                    _logger.RecordCSV(dynamicRecord);
//                }
//            }
//        }
//    }

//    public class PermissionsAllSiteSingleParameters : ISolutionParameters
//    {
//        public string SiteUrl { get; set; } = string.Empty;

//        public bool IncludeAdmins { get; set; } = true;
//        public bool IncludeSiteAccess { get; set; } = true;
//        public bool IncludeUniquePermissions { get; set; } = true;
//        public bool IncludeSubsites { get; set; } = true;

//        public bool IncludeSystemLists { get; set; } = false;
//        public bool IncludeResourceLists { get; set; } = false;

//    }
//}
