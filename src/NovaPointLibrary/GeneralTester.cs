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
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Solutions.Report;

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

        public async Task GetGroupMembersTest(string guid)
        {
            string graphAccessToken = await new GetAccessToken(_LogHelper, _appInfo).GraphTest();

            var collMembers = await new GetAzureADGroup(_LogHelper, _appInfo, graphAccessToken).GraphMembersAsync(guid);

            foreach (var oMember in collMembers)
            {
                _LogHelper.AddLogToUI($"{oMember.DisplayName}");
                _LogHelper.AddLogToUI($"{oMember.UserPrincipalName}");
            }
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

        public async Task<int> TestGetAllSubsites(string siteUrl)
        {
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string accessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(rootUrl);

            var collSubsites = new GetSubsite(_LogHelper, _appInfo, accessToken).CsomAllSubsitesWithRolesAndSiteDetails(siteUrl);


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
