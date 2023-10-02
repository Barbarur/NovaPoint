using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
using System.Diagnostics.Metrics;
using System.Dynamic;

namespace NovaPointLibrary.Solutions.Report
{
    public class PermissionsAllSiteSingleReport
    {
        public static string _solutionName = "Permissions in a Site report";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-PermissionAllSiteSingleReport"; 
        
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly string SiteUrl;
        
        private readonly bool IncludeAdmins;
        private readonly bool IncludeSiteAccess;
        private readonly bool IncludeUniquePermissions;
        private readonly bool IncludeSubsites;

        private readonly bool IncludeSystemLists;
        private readonly bool IncludeResourceLists;


        List<SPORoleAssignmentKnownGroup> KnownGroups { get; set; } = new() { };

        //private List<SPORoleAssignmentRecord> AssignmentUsers { get; set; } = new() { };

        //private string LocationName { get; set; } = String.Empty;
        //private string LocationURL { get; set; } = String.Empty;
        //private string LocationType { get; set; } = String.Empty;


        public PermissionsAllSiteSingleReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, PermissionsAllSiteSingleParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;

            SiteUrl = parameters.SiteUrl;

            IncludeAdmins = parameters.IncludeAdmins;
            IncludeSiteAccess = parameters.IncludeSiteAccess;
            IncludeUniquePermissions = parameters.IncludeUniquePermissions;
            IncludeSubsites = parameters.IncludeSubsites;

            IncludeSystemLists = parameters.IncludeSystemLists;
            IncludeResourceLists = parameters.IncludeResourceLists;
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
                {
                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Lists/Libraries";
                    Exception ex = new(message);
                    throw ex;
                }
                else if (IncludeAdmins == false && IncludeSiteAccess == false && IncludeUniquePermissions == false)
                {
                    string message = "FORM INCOMPLETED: YOu are not requesting any permissions. Report is Empty";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsyncNEW();
                }
            }
            catch (Exception ex)
            {
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }

        private async Task RunScriptAsyncNEW()
        {
            _appInfo.IsCancelled();
            _logHelper.ScriptStartNotice();


            string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
            string spoSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(rootUrl);
            string aadAccessToken = await new GetAccessToken(_logHelper, _appInfo).GraphInteractiveAsync();

            GetSPOSitePermissions getPermissions = new(_logHelper, _appInfo, spoSiteAccessToken, aadAccessToken, KnownGroups);

            Web oSite = new GetSPOSite(_logHelper, _appInfo, spoSiteAccessToken).CSOMWithRoles(SiteUrl);

            //double counter = 0;
            //double progress = Math.Round(counter * 100 / 1, 2);
            //counter++;
            //_logHelper.AddProgressToUI(progress);
            //_logHelper.AddLogToUI($"Processing Site '{oSite.Title}'");
            ProgressTracker progress = new(_logHelper, 1);
            progress.MainReportProgress($"Processing Site '{oSite.Title}'");

            AddRecordToCSV( await getPermissions.CSOMSiteAsync(oSite, IncludeAdmins, IncludeSiteAccess, IncludeUniquePermissions, IncludeSystemLists, IncludeResourceLists) );

            if (IncludeSubsites)
            {
                var collSubsites = new GetSubsite(_logHelper, _appInfo, spoSiteAccessToken).CsomAllSubsitesWithRolesAndSiteDetails(SiteUrl);
                progress.SubTaskProgressReset(collSubsites.Count);
                foreach (var oSubsite in collSubsites)
                {
                    //progress = Math.Round(counter * 100 / (collSubsites.Count + 1), 2);
                    //counter++;
                    //_logHelper.AddProgressToUI(progress);
                    //_logHelper.AddLogToUI($"Processing SubSite '{oSubsite.Title}'");
                    progress.SubTaskReportProgress($"Processing SubSite '{oSubsite.Title}'");
                    
                    AddRecordToCSV( await getPermissions.CSOMSubsiteAsync(oSubsite, IncludeSiteAccess, IncludeUniquePermissions, IncludeSystemLists, IncludeResourceLists) );

                    progress.SubTaskCounterIncrement();
                }
            }
            progress.MainCounterIncrement();

            _logHelper.ScriptFinishSuccessfulNotice();
        }

        private void AddRecordToCSV(List<SPOLocationPermissionsRecord> recordsList)
        {
            _appInfo.IsCancelled();
            _logHelper.AddLogToTxt($"[{GetType().Name}.AddRecordToCSV] - Adding record");
            
            foreach (var record in recordsList)
            {
                foreach (var roleAssigmentUser in record.SPORoleAssignmentUsersList)
                {
                    dynamic dynamicRecord = new ExpandoObject();
                    dynamicRecord.LocationType = record.LocationType;
                    dynamicRecord.LocationName = record.LocationName;
                    dynamicRecord.LocationUrl = record.LocationUrl;


                    dynamicRecord.AccessType = roleAssigmentUser.AccessType;
                    dynamicRecord.AccountType = roleAssigmentUser.AccountType;
                    dynamicRecord.Users = roleAssigmentUser.Users;
                    dynamicRecord.PermissionLevels = roleAssigmentUser.PermissionLevels;

                    dynamicRecord.Remarks = roleAssigmentUser.Remarks;

                    _logHelper.AddRecordToCSV(dynamicRecord);
                }
            }
        }
    }

    public class PermissionsAllSiteSingleParameters
    {
        internal string SiteUrl;

        public bool IncludeAdmins { get; set; } = true;
        public bool IncludeSiteAccess { get; set; } = true;
        public bool IncludeUniquePermissions { get; set; } = true;
        public bool IncludeSubsites { get; set; } = true;

        public bool IncludeSystemLists { get; set; } = false;
        public bool IncludeResourceLists { get; set; } = false;

        public PermissionsAllSiteSingleParameters(string siteUrl)
        {
            SiteUrl = siteUrl;
        }
    }
}
