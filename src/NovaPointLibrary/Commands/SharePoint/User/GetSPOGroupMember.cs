using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class GetSPOGroupMember
    {
        private LogHelper _LogHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOGroupMember(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _LogHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal UserCollection CSOMAllMembers(string siteUrl, string groupName)
        {
            if (_appInfo.CancelToken.IsCancellationRequested) { _appInfo.CancelToken.ThrowIfCancellationRequested(); };
            _LogHelper = new(_LogHelper, $"{GetType().Name}.CSOMAllMembers");
            _LogHelper.AddLogToTxt($"Start obtaining Group Members from Group'{groupName}'in site '{siteUrl}'");


            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.AadObjectId,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Id,
                u => u.Email,
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName),
                u => u.IsHiddenInUI,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.LoginName,
                u => u.PrincipalType,
                u => u.Title,
                u => u.UserId,
                u => u.UserPrincipalName
            };

            var group = clientContext.Web.SiteGroups.GetByName(groupName);
            UserCollection members = group.Users;

            clientContext.Load(group);
            clientContext.Load(members, m => m.Include(retrievalExpressions));
            clientContext.ExecuteQuery();

            _LogHelper.AddLogToTxt($"Finish obtaining Group Members from Group'{groupName}'");
            return members;
        }
    }
}
