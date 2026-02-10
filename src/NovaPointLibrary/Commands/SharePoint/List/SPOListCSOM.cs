using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.List
{

    internal class SPOListCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _defaultExpressions =
        [
            l => l.Hidden,
            l => l.IsSystemList,
            l => l.ParentWeb.Url,

            l => l.BaseType,
            l => l.DefaultViewUrl,
            l => l.Id,
            l => l.ItemCount,
            l => l.Title,
            l => l.RootFolder.ServerRelativeUrl,
        ];

        internal SPOListCSOM(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        // TO BE REMOVED AND REPACED BE GetAsync
        internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsyncAll(string siteUrl)
        {
            SPOListsParameters parameters = new()
            {
                AllLists = true,
                IncludeLibraries = true,
                IncludeLists = true,
                IncludeHiddenLists = true,
                IncludeSystemLists = true,
            };

            return await GetAsync(siteUrl, parameters);
        }

        internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsync(string siteUrl, SPOListsParameters parameters)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _logger.Info(methodName, $"Start getting Lists");

            if (parameters.AllLists)
            {
                var listCollection = await GetList(siteUrl, parameters.ListExpressions);

                List<Microsoft.SharePoint.Client.List> finalCollList = new();
                foreach (Microsoft.SharePoint.Client.List oList in listCollection)
                {
                    if (!parameters.IncludeHiddenLists && oList.Hidden == true) { continue; }

                    if (!parameters.IncludeSystemLists && oList.IsSystemList) { continue; }

                    if (!parameters.IncludeLibraries && oList.BaseType == BaseType.DocumentLibrary) { continue; }

                    if (!parameters.IncludeLists && oList.BaseType == BaseType.GenericList) { continue; }

                    // Excluded, User information can be retrieved Web.SiteUser
                    if (oList.Title == "User Information List") { continue; }

                    finalCollList.Add(oList);
                }

                _logger.Info(methodName, $"Finish filtering lists: {finalCollList.Count}");
                return finalCollList;

            }
            
            else if (!string.IsNullOrWhiteSpace(parameters.CollectionListsPath))
            {
                var listCollection = await GetList(siteUrl, parameters.ListExpressions);

                if (System.IO.File.Exists(parameters.CollectionListsPath))
                {
                    var matchingLists = listCollection
                        .Where(list => parameters.CollectionLists.Contains(list.Title, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    return matchingLists;
                }
                else
                {
                    throw new Exception("File with collection of lists doesn't exist.");
                }
            }

            else if (!string.IsNullOrWhiteSpace(parameters.ListTitle))
            {
                var list = await GetList(siteUrl, parameters.ListTitle, parameters.ListExpressions);

                List<Microsoft.SharePoint.Client.List> collList = [list];

                return collList;
            }
            
            else
            {
                throw new Exception("No list was required for this site");
            }

        }

        internal async Task<ListCollection> GetList(string siteUrl, Expression<Func<Microsoft.SharePoint.Client.List, object>>[] requestedExpressions)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start getting all lists");

            var expressions = _defaultExpressions.Union(requestedExpressions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            ListCollection collList = clientContext.Web.Lists;
            clientContext.Load(collList, l => l.Include(expressions));
            clientContext.ExecuteQueryRetry();

            _logger.Info(GetType().Name, $"Finish getting Lists: {collList.Count}");

            return collList;
        }


        internal async Task<Microsoft.SharePoint.Client.List> GetList(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.List, object>>[] requestedExpressions)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start getting list {listTitle}");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            var expressions = _defaultExpressions.Union(requestedExpressions).ToArray();

            Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(listTitle, expressions) ?? throw new Exception($"List '{listTitle}' not found");

            return list;
        }
    }
}
