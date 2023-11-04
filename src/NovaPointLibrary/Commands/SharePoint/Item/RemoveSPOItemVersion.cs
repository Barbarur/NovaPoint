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
    // TO BE DEPRECATED WHEN RemoveFileVersionAuto IS STABLE
    internal class RemoveSPOItemVersion
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;

        internal RemoveSPOItemVersion(NPLogger logger, AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Remove-PnPFileVersion.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Files/RemoveFileVersion.cs
        internal void CSOM(string siteUrl, string fileUrl, bool deleteAll = false, int versionId = -1, bool recycle = false)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            _logger.LogTxt(methodName, $"Start removing versions of the item '{fileUrl}'");

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
                var versions = file.Versions;

                if (deleteAll)
                {
                    _logger.LogTxt(methodName, $"Start deleting all the versions from '{fileUrl}'");
                    versions.DeleteAll();
                    clientContext.ExecuteQueryRetry();
                    _logger.LogTxt(methodName, $"Finish deleting all the versions from '{fileUrl}'");
                }
                else if (versionId != -1)
                {
                    if (recycle)
                    {
                        _logger.LogTxt(methodName, $"Start recycling version {versionId} from '{fileUrl}'");
                        versions.RecycleByID(versionId);
                    }
                    else
                    {
                        _logger.LogTxt(methodName, $"Start deleting version {versionId} from '{fileUrl}'");
                        versions.DeleteByID(versionId);
                    }
                    clientContext.ExecuteQueryRetry();
                    _logger.LogTxt(methodName, $"FInish removing versions of the item '{fileUrl}'");
                }
            }
            else
            {
                throw new Exception($"File '{fileUrl}' doesn't exist");
            }
        }
    }
}
