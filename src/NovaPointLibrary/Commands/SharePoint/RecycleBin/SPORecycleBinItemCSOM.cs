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

        //internal async Task<string> RemoveAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.RemoveAsync";
        //    _logger.LogTxt(methodName, $"Removing item {oRecycleBinItem.Title}");

        //    ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

        //    try
        //    {
        //        var ItemToDelete = clientContext.Site.RecycleBin.GetById(oRecycleBinItem.Id);

        //        ItemToDelete.DeleteObject();
        //        clientContext.ExecuteQueryRetry();
        //        return "Item removed from Recycle bin";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Recycle bin item", oRecycleBinItem.Title, ex);
        //        return ex.Message;
        //    }
        //}

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

        //internal async Task<string> RemoveRESTAPIAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Removing item {oRecycleBinItem.Title} using REST API");

        //    string api = siteUrl + "/_api/site/RecycleBin/DeleteByIds";

        //    string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

        //    try
        //    {
        //        await new RESTAPIHandler(_logger, _appInfo).Post(api, content);
        //        return "Item removed from Recycle bin";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Recycle bin item", oRecycleBinItem.Title, ex);
        //        return ex.Message;
        //    }
        //}





        // ORIGINAL METHOD TO KEEP AS RECORD
        //internal async Task<string> RestoreAsyncOriginal(string siteUrl, RecycleBinItem oRecycleBinItem, bool renameFile)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.RestoreAsync";
        //    _logger.LogTxt(methodName, $"Restoring item {oRecycleBinItem.Title} with id {oRecycleBinItem.Id}");

        //    ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

        //    try
        //    {
        //        var ItemToDelete = clientContext.Site.RecycleBin.GetById(oRecycleBinItem.Id);
        //        ItemToDelete.Restore();
        //        clientContext.ExecuteQueryRetry();
        //        return $"{oRecycleBinItem.ItemType} restored from Recycle bin";
        //    }
        //    catch (Exception ex)
        //    {
        //        if(ex.Message.Contains("To restore the file, rename the existing file and try again.") && renameFile)
        //        {
        //            if (oRecycleBinItem.ItemType != RecycleBinItemType.File && oRecycleBinItem.ItemType != RecycleBinItemType.Folder)
        //            {
        //                return $"{ex.Message} This is not neither a Folder or a Item. It cannot be renamed for restoration";
        //            }
        //            try
        //            {
        //                string remarks = await FileRenameAsync(siteUrl, oRecycleBinItem);
        //                remarks += " " + await RestoreAsync(siteUrl, oRecycleBinItem, renameFile);
        //                return remarks;
        //            }
        //            catch (Exception e)
        //            {
        //                Exception newEx = new($"{ex.Message} {e.Message}");
        //                _logger.ReportError("Recycle bin item", oRecycleBinItem.Title, newEx);
        //                return newEx.Message;
        //            }
        //        }
        //        else
        //        {
        //            _logger.ReportError("Recycle bin item", oRecycleBinItem.Title, ex);
        //            return ex.Message;
        //        }
        //    }
        //}












        //internal async Task<string> RestoreAsync(string siteUrl, RecycleBinItem oRecycleBinItem, bool renameFile)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.RestoreAsync";
        //    _logger.LogTxt(methodName, $"Start process to restore item {oRecycleBinItem.Title} with id {oRecycleBinItem.Id} using CSOM");

        //    return await RestoreRenameAsync(RestoreAsync, siteUrl, oRecycleBinItem, renameFile);
        //}

        //internal async Task<string> RestoreRESTAPIAsync(string siteUrl, RecycleBinItem oRecycleBinItem, bool renameFile)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Start process to restoring item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}' using REST API");

        //    return await RestoreRenameAsync(RestoreRESTAPIAsync, siteUrl, oRecycleBinItem, renameFile);
        //}

        //private async Task<string> RestoreRenameAsync(Func<string, RecycleBinItem, Task> restore, string siteUrl, RecycleBinItem oRecycleBinItem, bool renameFile)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.RestoreAsync";
        //    _logger.LogTxt(methodName, $"Processing restoration of item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}', renaming file '{renameFile}'");

        //    string IsRestored = $"{oRecycleBinItem.ItemType} restored from Recycle bin correctly";

        //    try
        //    {
        //        await restore(siteUrl, oRecycleBinItem);
        //        return IsRestored;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.Message.Contains("rename the existing file and try again.") && renameFile)
        //        {
        //            if (oRecycleBinItem.ItemType != RecycleBinItemType.File && oRecycleBinItem.ItemType != RecycleBinItemType.Folder)
        //            {
        //                return $"{ex.Message} This is not neither a Folder or a Item. It cannot be renamed for restoration";
        //            }
        //            try
        //            {
        //                string remarks = await FileRenameAsync(siteUrl, oRecycleBinItem);
        //                await restore(siteUrl, oRecycleBinItem);
        //                remarks += " " + IsRestored;
        //                return remarks;
        //            }
        //            catch (Exception e)
        //            {
        //                Exception newEx = new($"{ex.Message} {e.Message}");
        //                //_logger.ReportError("Recycle bin item", oRecycleBinItem.Title, newEx);
        //                return newEx.Message;
        //            }
        //        }
        //        else
        //        {
        //            //_logger.ReportError("Recycle bin item", oRecycleBinItem.Title, ex);
        //            return ex.Message;
        //        }
        //    }
        //}

        internal async Task RestoreAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Restoring item {oRecycleBinItem.Title} with id {oRecycleBinItem.Id} using CSOM");

            ClientContext clientContext = await _appInfo.GetContext(_logger, siteUrl);

            var ItemToDelete = clientContext.Site.RecycleBin.GetById(oRecycleBinItem.Id);
            ItemToDelete.Restore();
            clientContext.ExecuteQueryRetry();
        }

        //internal async Task<string> RestoreRESTAPIAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Restoring item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}' using REST API");

        //    string api = siteUrl + "/_api/site/RecycleBin/RestoreByIds";

        //    string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

        //    try
        //    {
        //        await new RESTAPIHandler(_logger, _appInfo).Post(api, content);
        //        return "Item removed from Recycle bin";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ReportError("Recycle bin item", oRecycleBinItem.Title, ex);
        //        return ex.Message;
        //    }
        //}

        //private async Task RestoreRESTAPIAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Restoring item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}' using REST API");

        //    string api = siteUrl + "/_api/site/RecycleBin/RestoreByIds";

        //    string content = $"{{'ids':['{oRecycleBinItem.Id}']}}";

        //    await new RESTAPIHandler(_logger, _appInfo).Post(api, content);
        //}














        //internal async Task<string> FileRenameAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}";
        //    _logger.LogTxt(methodName, $"Renaming original item {oRecycleBinItem.Title} in {siteUrl}");

        //    string unavailableName = oRecycleBinItem.Title;
        //    string newName;
        //    do
        //    {
        //        string itemNameOnly = Path.GetFileNameWithoutExtension(unavailableName);
        //        if (itemNameOnly[^3] == '(' 
        //            && int.TryParse(itemNameOnly[^2].ToString(), out int unit)
        //            && itemNameOnly[^1] == ')'
        //            && unit >= 9)
        //        {
        //            throw new Exception($"Too many {oRecycleBinItem.ItemType} with the same name on that location. We couldn't rename file from original location.");
        //        }

        //        newName = GetNewName(unavailableName);

        //        if (!await IsNameAvailable(siteUrl, oRecycleBinItem, newName))
        //        {
        //            unavailableName = newName;
        //            newName = string.Empty;
        //        }
        //    }
        //    while (string.IsNullOrWhiteSpace(newName));
            
        //    await RenameTargetLocationFile(siteUrl, oRecycleBinItem, newName);

        //    return $"File {oRecycleBinItem.Title} in the same location has been renamed as {newName}.";
        //}


        //internal string GetNewName(string itemName)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}";
        //    _logger.LogTxt(GetType().Name, $"Getting new name for file {itemName}");

        //    string itemNameOnly = Path.GetFileNameWithoutExtension(itemName);
        //    var extension = Path.GetExtension(itemName);

        //    bool isDuplicatedName = false;
        //    int unit = 1;
        //    if (itemNameOnly[^3] == '(' && int.TryParse(itemNameOnly[^2].ToString(), out unit) && itemNameOnly[^1] == ')')
        //    {
        //        isDuplicatedName = true;
        //    }

        //    string newName;
        //    if (isDuplicatedName)
        //    {
        //        unit++;
        //        string baseName = itemNameOnly.Substring(0, itemNameOnly.Length - 3);
        //        newName = baseName + $"({unit})";
        //    }
        //    else
        //    {
        //        newName = itemNameOnly + "(1)";
        //    }

        //    return newName + extension;
        //}


        //internal async Task<bool> IsNameAvailable(string siteUrl, RecycleBinItem oRecycleBinItem, string itemNewTitle)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Check if file with name '{itemNewTitle}' exists in original location");

        //    string itemRelativeUrl = UrlUtility.Combine(oRecycleBinItem.DirName, itemNewTitle);

        //    bool exists = true;

        //    if (oRecycleBinItem.ItemType == RecycleBinItemType.File)
        //    {
        //        var oFile = await new SPOFileCSOM(_logger, _appInfo).GetFileAsync(siteUrl, itemRelativeUrl);
        //        if (oFile.Exists) { exists = false; }
        //    }
        //    else if (oRecycleBinItem.ItemType == RecycleBinItemType.Folder)
        //    {
        //        var oFolder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(siteUrl, itemRelativeUrl);
        //        if (oFolder.Exists) { exists = false; }
        //    }

        //    return exists;

        //}

        //internal async Task RenameTargetLocationFile(string siteUrl, RecycleBinItem oRecycleBinItem, string newName)
        //{
        //    _appInfo.IsCancelled();
        //    _logger.LogTxt(GetType().Name, $"Check if file with name '{newName}' exists in original location");

        //    string itemRelativeUrl = UrlUtility.Combine(oRecycleBinItem.DirName, oRecycleBinItem.Title);

        //    try
        //    {
        //        SPOListItemCSOM item = new(_logger, _appInfo);
        //        if (oRecycleBinItem.ItemType == RecycleBinItemType.File)
        //        {
        //            await new SPOFileCSOM(_logger, _appInfo).RenameFileAsync(siteUrl, itemRelativeUrl, newName);
        //        }
        //        else if (oRecycleBinItem.ItemType == RecycleBinItemType.Folder)
        //        {
        //            await new SPOFolderCSOM(_logger, _appInfo).RenameFolderAsync(siteUrl, itemRelativeUrl, newName);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Exception newEx = new($"Error while renaming file '{oRecycleBinItem.Title}' as '{newName}' at '{oRecycleBinItem.DirName}'. {ex.Message}");
        //        throw newEx;
        //    }
        //}
    }
}
