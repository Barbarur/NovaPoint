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
        //private readonly Main _main;
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        //internal SPOListCSOM(Main main)
        //{
        //    _main = main;
        //}

        internal SPOListCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        //internal async Task<List<Microsoft.SharePoint.Client.List>> GetDEPRECATED(string siteUrl,
        //                                                                string? listName,
        //                                                                bool includeHiddenLists,
        //                                                                bool includeSystemLists)
        //{
        //    _main.IsCancelled();

        //    var expressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //    {
        //    };

        //    return await GetDEPRECATED(siteUrl, listName, includeHiddenLists, includeSystemLists, expressions);
        //}

        //internal async Task<List<Microsoft.SharePoint.Client.List>> GetDEPRECATED(string siteUrl,
        //                                                                string? listName,
        //                                                                bool includeHiddenLists,
        //                                                                bool includeSystemLists,
        //                                                                Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievalExpressions)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.Get";
        //    _main.AddLogToTxt(methodName, $"Start getting Lists");

        //    var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //    {
        //        l => l.Hidden,
        //        l => l.IsSystemList,
        //        l => l.ParentWeb.Url,

        //        l => l.BaseType,
        //        l => l.DefaultViewUrl,
        //        l => l.Id,
        //        l => l.ItemCount,
        //        l => l.Title,
        //    };

        //    var expressions = defaultExpressions.Union(retrievalExpressions).ToArray();

        //    ClientContext clientContext = await _main.GetContext(siteUrl);

        //    if (!String.IsNullOrWhiteSpace(listName))
        //    {
        //        Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(listName, expressions);
        //        List<Microsoft.SharePoint.Client.List> collList = new() { list };
        //        return collList;
        //    }
        //    else
        //    {
        //        ListCollection collList = clientContext.Web.Lists;
        //        clientContext.Load(collList, l => l.Include(expressions));
        //        clientContext.ExecuteQuery();

        //        _main.AddLogToTxt(methodName, $"Finish getting Lists: {collList.Count}");

        //        List<Microsoft.SharePoint.Client.List> finalCollList = new();
        //        foreach (Microsoft.SharePoint.Client.List oList in collList)
        //        {
        //            if (oList.Hidden == true && !includeHiddenLists) { continue; }

        //            if (oList.IsSystemList && !includeSystemLists) { continue; }

        //            finalCollList.Add(oList);
        //        }

        //        _main.AddLogToTxt(methodName, $"Finish filtering lists. Count: {finalCollList.Count}");

        //        return finalCollList;
        //    }
        //}

        //internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsync(string siteUrl,
        //                                                                string? listName,
        //                                                                bool includeHiddenLists,
        //                                                                bool includeSystemLists)
        //{
        //    _appInfo.IsCancelled();

        //    var expressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //    {
        //    };

        //    return await GetAsync(siteUrl, listName, includeHiddenLists, includeSystemLists, expressions);
        //}

        //internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsync(string siteUrl,
        //                                                                string? listName,
        //                                                                bool includeHiddenLists,
        //                                                                bool includeSystemLists,
        //                                                                Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievalExpressions)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.Get";
        //    _logger.LogTxt(methodName, $"Start getting Lists");

        //    var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        //    {
        //        l => l.Hidden,
        //        l => l.IsSystemList,
        //        l => l.ParentWeb.Url,

        //        l => l.BaseType,
        //        l => l.DefaultViewUrl,
        //        l => l.Id,
        //        l => l.ItemCount,
        //        l => l.Title,
        //    };

        //    var expressions = defaultExpressions.Union(retrievalExpressions).ToArray();

        //    ClientContext clientContext = await _appInfo.GetContext(siteUrl);

        //    if (!String.IsNullOrWhiteSpace(listName))
        //    {
        //        Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(listName, expressions);
        //        List<Microsoft.SharePoint.Client.List> collList = new() { list };

        //        _logger.LogTxt(GetType().Name, $"Collected list '{listName}'");
        //        return collList;
        //    }
        //    else
        //    {
        //        ListCollection collList = clientContext.Web.Lists;
        //        clientContext.Load(collList, l => l.Include(expressions));
        //        clientContext.ExecuteQuery();

        //        _logger.LogTxt(methodName, $"Finish getting Lists: {collList.Count}");

        //        List<Microsoft.SharePoint.Client.List> finalCollList = new();
        //        foreach (Microsoft.SharePoint.Client.List oList in collList)
        //        {
        //            if (oList.Hidden == true && !includeHiddenLists) { continue; }

        //            if (oList.IsSystemList && !includeSystemLists) { continue; }
        //            finalCollList.Add(oList);
        //        }

        //        _logger.LogTxt(methodName, $"Finish filtering lists: {finalCollList.Count}");

        //        return finalCollList;
        //    }
        //}

        internal async Task<List<Microsoft.SharePoint.Client.List>> GetAsync(string siteUrl,
                                                                             SPOTenantListsParameters parameters)
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
            };

            var expressions = defaultExpressions.Union(parameters.ListExpresions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            if (!String.IsNullOrWhiteSpace(parameters.ListTitle))
            {
                Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(parameters.ListTitle, expressions);
                List<Microsoft.SharePoint.Client.List> collList = new() { list };

                _logger.LogTxt(GetType().Name, $"Collected list '{parameters.ListTitle}'");
                return collList;
            }
            else
            {
                ListCollection collList = clientContext.Web.Lists;
                clientContext.Load(collList, l => l.Include(expressions));
                clientContext.ExecuteQuery();

                _logger.LogTxt(methodName, $"Finish getting Lists: {collList.Count}");

                List<Microsoft.SharePoint.Client.List> finalCollList = new();
                foreach (Microsoft.SharePoint.Client.List oList in collList)
                {
                    if (oList.Hidden == true && !parameters.IncludeHiddenLists) { continue; }

                    if (oList.IsSystemList && !parameters.IncludeSystemLists) { continue; }
                    finalCollList.Add(oList);
                }

                _logger.LogTxt(methodName, $"Finish filtering lists: {finalCollList.Count}");

                return finalCollList;
            }
        }
    }
}
