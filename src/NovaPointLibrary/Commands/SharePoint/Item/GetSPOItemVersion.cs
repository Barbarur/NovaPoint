using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using PnP.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class GetSPOItemVersion
    {
        private readonly LogHelper _logHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOItemVersion(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Get-PnPFileVersion.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Files/GetFileVersion.cs
        internal FileVersionCollection CSOM(string siteUrl, string fileUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            _logHelper.AddLogToTxt(methodName, $"Start getting all version of the item '{fileUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            string serverRelativeUrl = string.Empty;

            var webUrl = clientContext.Web.EnsureProperty(w => w.ServerRelativeUrl);

            if (!fileUrl.ToLower().StartsWith(webUrl.ToLower()))
            {
                serverRelativeUrl = UrlUtility.Combine(webUrl, fileUrl);
            }
            else
            {
                serverRelativeUrl = fileUrl;
            }

            Microsoft.SharePoint.Client.File file;

            file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(serverRelativeUrl));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            clientContext.ExecuteQueryRetry();

            if (file.Exists)
            {
                FileVersionCollection versions = file.Versions;
                clientContext.ExecuteQueryRetry();

                _logHelper.AddLogToTxt(methodName, $"Finish getting all version of the item '{fileUrl}'");
                return versions;
            }
            else
            {
                throw new Exception($"File '{fileUrl}' doesn't exist");
            }
        }
    }
}
