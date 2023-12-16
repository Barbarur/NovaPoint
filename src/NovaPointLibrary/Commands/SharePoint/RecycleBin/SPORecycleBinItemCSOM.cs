using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Solutions;
using PnP.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using File = Microsoft.SharePoint.Client.File;

namespace NovaPointLibrary.Commands.SharePoint.RecycleBin
{
    internal class SPORecycleBinItemCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPORecycleBinItemCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private async IAsyncEnumerable<RecycleBinItemCollection> GetBatchAsync(string siteUrl, RecycleBinItemState recycleBinStage)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.GetBatchAsync";
            _logger.LogTxt(methodName, $"Start getting Items from the Recycle Bin from {siteUrl}");

            
            string? pagingInfo = null;
            RecycleBinItemCollection recycleBinItemCollection;
            do
            {
                ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

                recycleBinItemCollection = clientContext.Site.GetRecycleBinItems(pagingInfo, 5000, false, RecycleBinOrderBy.DefaultOrderBy, recycleBinStage);
                clientContext.Load(recycleBinItemCollection);
                clientContext.ExecuteQueryRetry();

                // Reference:
                // https://www.portiva.nl/portiblog/blogs-cat/paging-through-sharepoint-recycle-bin
                if (recycleBinItemCollection.Count > 0)
                {
                    var nextId = recycleBinItemCollection.Last().Id;
                    var nextTitle = WebUtility.UrlEncode(recycleBinItemCollection.Last().Title);
                    pagingInfo = $"id={nextId}&title={nextTitle}";
                }

                yield return recycleBinItemCollection;
            }
            while (recycleBinItemCollection?.Count == 5000);

            _logger.LogTxt(methodName, $"Finish getting Items from the Recycle Bin from {siteUrl}");
        }

        internal async IAsyncEnumerable<RecycleBinItem> GetAsync(string siteUrl, SPORecycleBinItemParameters parameters)
        {
            _appInfo.IsCancelled();

            int counter = 0 ;
            if (parameters.FirstStage)
            {
                await foreach (var recycleBinItemCollection in GetBatchAsync(siteUrl, RecycleBinItemState.FirstStageRecycleBin))
                {
                    counter += recycleBinItemCollection.Count;
                    _logger.LogUI(GetType().Name, $"Collected {counter} items from the recycle bin");
                    foreach (var oRecycleBinItem in recycleBinItemCollection)
                    {
                        if (MatchParameters(oRecycleBinItem, parameters)) { yield return oRecycleBinItem; }
                    }
                }
            }

            if (parameters.SecondStage)
            {
                await foreach (var recycleBinItemCollection in GetBatchAsync(siteUrl, RecycleBinItemState.SecondStageRecycleBin))
                {
                    counter += recycleBinItemCollection.Count;
                    _logger.LogUI(GetType().Name, $"Collected {counter} items from the recycle bin");
                    foreach (var oRecycleBinItem in recycleBinItemCollection)
                    {
                        if (MatchParameters(oRecycleBinItem, parameters)) { yield return oRecycleBinItem; }
                    }
                }
            }
        }

        internal bool MatchParameters(RecycleBinItem oRecycleBinItem, SPORecycleBinItemParameters p)
        {
            _appInfo.IsCancelled(); 
            
            bool match;

            bool date;
            if (oRecycleBinItem.DeletedDate.CompareTo(p.DeletedAfter) > 0 && 0 > oRecycleBinItem.DeletedDate.CompareTo(p.DeletedBefore)) {
                date = true;
            }
            else { date = false;  }

            bool email;
            if (!string.IsNullOrWhiteSpace(p.DeletedByEmail))
            {
                if (oRecycleBinItem.DeletedByEmail.Equals(p.DeletedByEmail, StringComparison.OrdinalIgnoreCase)) { email = true; }
                else { email = false; }
            }
            else { email = true; }

            bool location;
            if (!string.IsNullOrWhiteSpace(p.OriginalLocation))
            {
                if (oRecycleBinItem.DirName.Contains(p.OriginalLocation)) { location = true; }
                else { location = false; }
            }
            else { location = true; }
            
            bool size;
            if (p.FileSizeMb > 0)
            {
                if (p.FileSizeAbove && Math.Round(oRecycleBinItem.Size / Math.Pow(1024, 2), 2) > p.FileSizeMb) { size = true; }
                else if (!p.FileSizeAbove && Math.Round(oRecycleBinItem.Size / Math.Pow(1024, 2), 2) < p.FileSizeMb) { size = true; }
                else { size = false; }
            }
            else { size = true; }


            if(date && email && location && size) { match = true; }
            else { match = false; }

            return match;
        }

        internal async Task RemoveAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.RemoveAsync";
            _logger.LogTxt(methodName, $"Removing item {oRecycleBinItem.Title}");

            ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

            var ItemToDelete = clientContext.Site.RecycleBin.GetById(oRecycleBinItem.Id);

            ItemToDelete.DeleteObject();
            clientContext.ExecuteQueryRetry();
        }

        internal async Task RestoreAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Restoring item {oRecycleBinItem.Title} with id {oRecycleBinItem.Id} using CSOM");

            ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

            var ItemToDelete = clientContext.Site.RecycleBin.GetById(oRecycleBinItem.Id);
            ItemToDelete.Restore();
            clientContext.ExecuteQueryRetry();
        }
    }
}
