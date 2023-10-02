using Microsoft.Graph;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.List
{


    internal class SPOListCSOM
    {
        private readonly Main _main;

        internal SPOListCSOM(Main main)
        {
            _main = main;
        }


        internal async Task<List<Microsoft.SharePoint.Client.List>> Get(string siteUrl,
                                                                        string? listName,
                                                                        bool includeHiddenLists,
                                                                        bool includeSystemLists)
        {
            _main.IsCancelled();

            var expressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
            {
            };

            return await Get(siteUrl, listName, includeHiddenLists, includeSystemLists, expressions);
        }

        internal async Task<List<Microsoft.SharePoint.Client.List>> Get(string siteUrl,
                                                                        string? listName,
                                                                        bool includeHiddenLists,
                                                                        bool includeSystemLists,
                                                                        Expression<Func<Microsoft.SharePoint.Client.List, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _main.AddLogToTxt(methodName, $"Start getting Lists");

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

            var expressions = defaultExpressions.Union(retrievalExpressions).ToArray();

            ClientContext clientContext = await _main.GetContext(siteUrl);

            if (!String.IsNullOrWhiteSpace(listName))
            {
                Microsoft.SharePoint.Client.List list = clientContext.Web.GetListByTitle(listName, expressions);
                List<Microsoft.SharePoint.Client.List> collList = new() { list };
                return collList;
            }
            else
            {
                ListCollection collList = clientContext.Web.Lists;
                clientContext.Load(collList, l => l.Include(expressions));
                clientContext.ExecuteQuery();

                _main.AddLogToTxt(methodName, $"Finish getting Lists: {collList.Count}");

                List<Microsoft.SharePoint.Client.List> finalCollList = new();
                foreach (Microsoft.SharePoint.Client.List oList in collList)
                {
                    if (oList.Hidden == true && !includeHiddenLists) { continue; }
                    
                    if (oList.IsSystemList && !includeSystemLists) { continue; }

                    finalCollList.Add(oList);
                }
                
                _main.AddLogToTxt(methodName, $"Finish filtering lists. Count: {finalCollList.Count}");

                return finalCollList;
            }
        }
    }
}
