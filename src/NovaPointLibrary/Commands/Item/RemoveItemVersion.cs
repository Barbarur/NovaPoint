using Microsoft.SharePoint.Client;
using PnP.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Item
{
    internal class RemoveItemVersion
    {
        private readonly Action<string, string> AddLog;
        private readonly string AccessToken;
        internal RemoveItemVersion(Action<string, string> addLog, string accessToken)
        {
            AddLog = addLog;
            AccessToken = accessToken;
        }

        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Remove-PnPFileVersion.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Files/RemoveFileVersion.cs
        internal void Csom(string siteUrl, string fileUrl, bool deleteAll = false, int versionId = -1, bool recycle = false)
        {
            AddLog($"[{GetType().Name}.Csom]", $"Getting all version of the item '{fileUrl}'");

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
                AddLog($"[{GetType().Name}.Csom]", $"File '{fileUrl}' exist");
                var versions = file.Versions;

                if (deleteAll)
                {
                    AddLog($"[{GetType().Name}.Csom]", $"Start deleting all the versions from '{fileUrl}'");
                    versions.DeleteAll();
                    clientContext.ExecuteQueryRetry();
                    AddLog($"[{GetType().Name}.Csom]", $"Successfully deleted all the versions from '{fileUrl}'");
                }
                else if (versionId != -1)
                {
                    if (recycle)
                    {
                        AddLog($"[{GetType().Name}.Csom]", $"Start deleting version {versionId} from '{fileUrl}'");
                        versions.RecycleByID(versionId);
                    }
                    else
                    {
                        AddLog($"[{GetType().Name}.Csom]", $"Start deleting version {versionId} from '{fileUrl}'");
                        versions.DeleteByID(versionId);
                    }
                    clientContext.ExecuteQueryRetry();
                    AddLog($"[{GetType().Name}.Csom]", $"Successfully deleted version {versionId} from '{fileUrl}'");
                }
            }
            else
            {
                AddLog($"[{GetType().Name}.Csom]", $"File '{fileUrl}' doesn't exist");
                throw new Exception($"File '{fileUrl}' doesn't exist");
            }
        }
    }
}
