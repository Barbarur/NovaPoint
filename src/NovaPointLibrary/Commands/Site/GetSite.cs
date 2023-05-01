using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Site
{
    internal class GetSite
    {
        private LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;

        internal GetSite(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal Web Csom(string siteUrl)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.Csom");

            _logHelper.AddLogToTxt($"Getting Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Web, object>>[]
            { 
                w => w.Id,
                w => w.Url,
                w => w.Title,
                w => w.ServerRelativeUrl,
            };

            clientContext.Web.EnsureProperties(retrievalExpressions);

            return clientContext.Web;
        }
    }
}
