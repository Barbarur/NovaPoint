using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCSOM
    {
        //private readonly Main _main;
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        
        //internal SPOSiteCSOM(Main main)
        //{
        //    _main = main;
        //}
        internal SPOSiteCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        //internal async Task<Web> GetToDeprecate(string siteUrl)
        //{
        //    _main.IsCancelled();

        //    var expresions = new Expression<Func<Web, object>>[]
        //    {
        //    };

        //    return await GetToDeprecate(siteUrl, expresions);
        //}

        //internal async Task<Web> GetToDeprecate(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.Get";
        //    _main.AddLogToTxt(methodName, $"Start getting Site '{siteUrl}'");

        //    var defaultExpressions = new Expression<Func<Web, object>>[]
        //    {
        //        w => w.Id,
        //        w => w.Title,
        //        w => w.Url,
        //    };

        //    var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

        //    ClientContext clientContext = await _main.GetContext(siteUrl);

        //    clientContext.Web.EnsureProperties(expressions);

        //    _main.AddLogToTxt(methodName, $" Finish getting Site '{siteUrl}'");
        //    return clientContext.Web;
        //}

        internal async Task<Web> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            var expresions = new Expression<Func<Web, object>>[]
            {
            };

            return await GetAsync(siteUrl, expresions);
        }

        internal async Task<Web> GetAsync(string siteUrl, Expression<Func<Web, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Get";
            _logger.LogTxt(methodName, $"Start getting Site '{siteUrl}'");

            var defaultExpressions = new Expression<Func<Web, object>>[]
            {
                w => w.Id,
                w => w.Title,
                w => w.Url,
            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.EnsureProperties(expressions);

            _logger.LogTxt(methodName, $" Finish getting Site '{siteUrl}'");
            return clientContext.Web;
        }
    }
}
