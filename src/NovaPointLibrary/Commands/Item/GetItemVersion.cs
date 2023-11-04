//using Microsoft.SharePoint.Client;
//using PnP.Framework.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.Item
//{
//    // TO BE DEPRECATED ONCE ItemAllListSingleSiteSingleReport IS ON PRODUCTION
//    internal class GetItemVersion
//    {
//        private readonly Action<string, string> AddLog;
//        private readonly string AccessToken;
//        internal GetItemVersion(Action<string, string> addLog, string accessToken)
//        {
//            AddLog = addLog;
//            AccessToken = accessToken;
//        }

//        // Reference:
//        // https://pnp.github.io/powershell/cmdlets/Get-PnPFileVersion.html
//        // https://github.com/pnp/powershell/blob/dev/src/Commands/Files/GetFileVersion.cs
//        internal FileVersionCollection Csom(string siteUrl, string fileUrl)
//        {
//            AddLog($"[{GetType().Name}.CsomAllItems]", $"Getting all version of the item '{fileUrl}'");

//            using var clientContext = new ClientContext(siteUrl);
//            clientContext.ExecutingWebRequest += (sender, e) =>
//            {
//                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
//            };

//            string serverRelativeUrl = string.Empty;

//            var webUrl = clientContext.Web.EnsureProperty(w => w.ServerRelativeUrl);

//            if (!fileUrl.ToLower().StartsWith(webUrl.ToLower()))
//            {
//                serverRelativeUrl = UrlUtility.Combine(webUrl, fileUrl);
//            }
//            else
//            {
//                serverRelativeUrl = fileUrl;
//            }

//            Microsoft.SharePoint.Client.File file;

//            file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(serverRelativeUrl));

//            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
//            clientContext.ExecuteQueryRetry();

//            if (file.Exists)
//            {
//                FileVersionCollection versions = file.Versions;
//                clientContext.ExecuteQueryRetry();

//                return versions;
//            }
//            else
//            {
//                throw new Exception($"File '{fileUrl}' doesn't exist");
//            }
//        }
//    }
//}
