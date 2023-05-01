using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Commands.Site;
using System.Diagnostics.Metrics;

namespace NovaPointLibrary
{
    public class GeneralTester
    {
        // Baic parameters required for all reports
        private LogHelper _LogHelper;
        private readonly AppInfo _appInfo;

        public GeneralTester(Action<LogInfo> uiAddLog, AppInfo appInfo)
        {
            // Baic parameters required for all reports
            _LogHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
        }

        public async Task GetUser(string siteUrl, string UserUPN)
        {
            _LogHelper.ScriptStartNotice();

            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveNoTenatIdAsync(rootUrl);

            User? user = new GetUser(_LogHelper, rootSiteAccessToken).CsomSingle(siteUrl, UserUPN);

            if (user != null)
            {
                _LogHelper.AddLogToTxt("USER FOUND!!!");
                _LogHelper.AddLogToTxt($"{user.LoginName}");
            }
            else
            {
                _LogHelper.AddLogToTxt("USER NO FOUND");
            }

        }

        public async Task TestGetSite(string siteUrl)
        {
            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string accessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(rootUrl);

            var site = new GetSite(_LogHelper, _appInfo, accessToken).Csom(siteUrl);

            _LogHelper.AddLogToUI($"Site Unique {site.HasUniqueRoleAssignments}");
            _LogHelper.AddLogToUI($"Site RoleAssignments {site.RoleAssignments}");

            foreach (var role in site.RoleAssignments)
            {
                _LogHelper.AddLogToUI($"Site Role: {role.RoleDefinitionBindings}");
                //_LogHelper.AddLogToUI($"Site Role: {role.Member.PrincipalType}");
                //_LogHelper.AddLogToUI($"Site Role: {role.Member.Title}");

                //foreach (var binding in role.RoleDefinitionBindings)
                //{
                //    _LogHelper.AddLogToUI($"Site Role: {binding}");
                //    //_LogHelper.AddLogToUI($"Site Role: {role.Member.PrincipalType}");
                //    //_LogHelper.AddLogToUI($"Site Role: {role.Member.Title}");
                //}

            }
        }

        public async Task<int> TestGetAllSubsites(string siteUrl)
        {
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string accessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(rootUrl);

            var collSubsites = new GetSubsite(_LogHelper, _appInfo, accessToken).CsomAllSubsitesWithRoles(siteUrl);


            //foreach (var oSubsite in collSubsites)
            //{
                //_LogHelper.AddLogToUI($"Subsite: {oSubsite.Url}");
                //_LogHelper.AddLogToUI($"Site Unique {oSubsite.HasUniqueRoleAssignments}");

                //foreach (var role in oSubsite.RoleAssignments)
                //{
                //    _LogHelper.AddLogToUI($"Site RoleAssignments.Member.Title {role.Member.Title}");
                //    _LogHelper.AddLogToUI($"Site RoleAssignments.Member.PrincipalType {role.Member.PrincipalType}");
                    
                //    foreach (var roleDef in role.RoleDefinitionBindings)
                //    {
                //        _LogHelper.AddLogToUI($"Site RoleAssignmentsRoleDefinitionBindings.Name {roleDef.Name}");

                //    }

                //}

                //_LogHelper.AddLogToUI("");
            //}

            watch.Stop();

            _LogHelper.AddLogToUI($"Execution Time: {watch.ElapsedMilliseconds} ms");

            return (int)watch.ElapsedMilliseconds;
        }
    }
}
