using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{

    internal class SPOListCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOListCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsync(string siteUrl,
                                                                             SPOListsParameters parameters)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _logger.LogTxt(methodName, $"Start getting Lists");

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
            {
                l => l.Hidden,
                l => l.IsSystemList,
                l => l.ParentWeb.Url,

                l => l.BaseType,
                l => l.DefaultViewUrl,
                l => l.Id,
                l => l.ItemCount,
                l => l.Title,
                l => l.RootFolder.ServerRelativeUrl,
            };

            var expressions = defaultExpressions.Union(parameters.ListExpresions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            if (parameters.AllLists)
            {
                ListCollection collList = clientContext.Web.Lists;
                clientContext.Load(collList, l => l.Include(expressions));
                clientContext.ExecuteQuery();

                _logger.LogTxt(methodName, $"Finish getting Lists: {collList.Count}");

                List<Microsoft.SharePoint.Client.List> finalCollList = new();
                foreach (Microsoft.SharePoint.Client.List oList in collList)
                {
                    if (!parameters.IncludeHiddenLists && oList.Hidden == true) { continue; }

                    if (!parameters.IncludeSystemLists && oList.IsSystemList) { continue; }

                    if (!parameters.IncludeLibraries && oList.BaseType == BaseType.DocumentLibrary) { continue; }

                    if (!parameters.IncludeLists && oList.BaseType == BaseType.GenericList) { continue; }

                    // Excluded, User information can be retrieved Web.SiteUser
                    if (oList.Title == "User Information List") { continue; }

                    finalCollList.Add(oList);
                }

                _logger.LogTxt(methodName, $"Finish filtering lists: {finalCollList.Count}");

                return finalCollList;
            }

            else
            {
                Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(parameters.ListTitle, expressions);
                if (list == null)
                {
                    throw new Exception($"List '{parameters.ListTitle}' not found");
                }

                List<Microsoft.SharePoint.Client.List> collList = new() { list };

                _logger.LogTxt(GetType().Name, $"Collected list '{list.Title}'");
                return collList;
            }
        }
    }
}
