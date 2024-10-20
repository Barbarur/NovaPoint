using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using PnP.Framework.Utilities;
using System.Dynamic;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RestoreRecycleBinAuto : ISolution
    {
        public static readonly string s_SolutionName = "Restore items from recycle bin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RestoreRecycleBinAuto";

        private RestoreRecycleBinAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;

        private RestoreRecycleBinAuto(LoggerSolution logger, AppInfo appInfo, RestoreRecycleBinAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RestoreRecycleBinAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "RestoreRecycleBinAuto", parameters);
            try
            {
                AppInfo appInfo = await AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RestoreRecycleBinAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }
        //public RestoreRecycleBinAuto(RestoreRecycleBinAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{;
        //    _param = parameters;
        //    _logger = new(uiAddLog, this.GetType().Name, _param);
        //    _appInfo = new(_logger, cancelTokenSource);
        //}

        //public async Task RunAsync()
        //{
        //    try
        //    {
        //        await RunScriptAsync();

        //        _logger.ScriptFinish();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ScriptFinish(ex);
        //    }
        //}

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if ( siteResults.Ex != null)
                {
                    _logger.Error(GetType().Name, "Site", siteResults.SiteUrl, siteResults.Ex);
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Ex.Message);
                    continue;
                }


                if (_param.RecycleBinParam.AllItems)
                {
                    try
                    {
                        await RestoreAllRecycleBinItemsAsync(siteResults.SiteUrl);
                    }
                    catch (Exception ex)
                    {
                        if(ex.Message.Contains("The attempted operation is prohibited because it exceeds the list view threshold"))
                        {
                            _param.RecycleBinParam.AllItems = false;
                            _logger.UI(GetType().Name, "Recycle bin items cannot be restored in bulk due view threshold limitation. Recycle bin items will be restored individually which might take a bit longer to finish.");
                        }
                        else if (ex.Message.Contains("rename the existing") && _param.RenameFile)
                        {
                            _param.RecycleBinParam.AllItems = false;
                            _logger.UI(GetType().Name, "Recycle bin items cannot be restored in bulk due some files and folders with the same name already existing in the target location. Recycle bin items will be restored individually which might take a bit longer to finish.");
                        }
                        else
                        {
                            _logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                            AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                        }
                    }
                }

                if (!_param.RecycleBinParam.AllItems)
                {
                    try
                    {
                        await ProcessRecycleBinItemsAsync(siteResults.SiteUrl, siteResults.Progress);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                        AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                    }
                }
            }
        }

        private async Task RestoreAllRecycleBinItemsAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            clientContext.Web.RecycleBin.RestoreAll();
            clientContext.ExecuteQueryRetry();
            AddRecord(siteUrl, remarks: "All recycle bin items have been restored");
        }

        private async Task ProcessRecycleBinItemsAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            
            ProgressTracker progress = new(parentProgress, 5000);
            int itemCounter = 0;
            int itemExpectedCount = 5000;
            var spoRecycleBinItem = new SPORecycleBinItemCSOM(_logger, _appInfo);
            await foreach (RecycleBinItem oRecycleBinItem in spoRecycleBinItem.GetAsync(siteUrl, _param.RecycleBinParam))
            {
                _appInfo.IsCancelled();

                string remarks = string.Empty;

                try
                {
                    remarks = await RestoreRenameAsync(new SPORecycleBinItemREST(_logger, _appInfo).RestoreAsync, siteUrl, oRecycleBinItem);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Recycle bin item", oRecycleBinItem.Title, ex);
                    remarks = ex.Message;
                }

                AddRecord(siteUrl, oRecycleBinItem, remarks);

                progress.ProgressUpdateReport();
                itemCounter++;
                if (itemCounter == itemExpectedCount)
                {
                    progress.IncreaseTotalCount(6000);
                    itemExpectedCount += 5000;
                }
            }

            _logger.Info(GetType().Name, $"Finish processing recycle bin items for '{siteUrl}'");
        }

        private async Task<string> RestoreRenameAsync(Func<string, RecycleBinItem, Task> restore, string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Processing restoration of item '{oRecycleBinItem.Title}' with id '{oRecycleBinItem.Id}', renaming file '{_param.RenameFile}'");

            string isRestored = $"'{oRecycleBinItem.ItemType}' restored from Recycle bin correctly";

            try
            {
                await restore(siteUrl, oRecycleBinItem);
                return isRestored;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("rename the existing") && _param.RenameFile)
                {
                    if (oRecycleBinItem.ItemType != RecycleBinItemType.File && oRecycleBinItem.ItemType != RecycleBinItemType.Folder)
                    {
                        return $"{ex.Message} This is not neither a Folder or a Item. It cannot be renamed for restoration";
                    }

                    string remarks = string.Empty;

                    try
                    {
                        remarks = await FileRenameAsync(siteUrl, oRecycleBinItem);
                        await restore(siteUrl, oRecycleBinItem);
                        remarks += " " + isRestored;
                        return remarks;
                    }
                    catch (Exception e)
                    {
                        Exception newEx = new($"{ex.Message} {remarks} {e.Message}");
                        throw newEx;
                    }
                }
                else
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        internal async Task<string> FileRenameAsync(string siteUrl, RecycleBinItem oRecycleBinItem)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Renaming original item {oRecycleBinItem.Title} in {siteUrl}");

            string unavailableName = oRecycleBinItem.Title;
            string newName;
            do
            {
                string itemNameOnly = Path.GetFileNameWithoutExtension(unavailableName);
                if (itemNameOnly[^3] == '('
                    && int.TryParse(itemNameOnly[^2].ToString(), out int unit)
                    && itemNameOnly[^1] == ')'
                    && unit >= 9)
                {
                    throw new Exception($"Too many {oRecycleBinItem.ItemType} with the same name on that location. We couldn't rename file from original location.");
                }

                newName = GetNewName(unavailableName);

                if (!await IsNameAvailable(siteUrl, oRecycleBinItem, newName))
                {
                    unavailableName = newName;
                    newName = string.Empty;
                }
            }
            while (string.IsNullOrWhiteSpace(newName));

            await RenameTargetLocationFile(siteUrl, oRecycleBinItem, newName);

            return $"File {oRecycleBinItem.Title} in the same location has been renamed as {newName}.";
        }


        internal string GetNewName(string itemName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting new name for item '{itemName}'");

            string itemNameOnly = Path.GetFileNameWithoutExtension(itemName);
            var extension = Path.GetExtension(itemName);

            bool isDuplicatedName = false;
            int unit = 1;
            if (itemNameOnly[^3] == '(' && int.TryParse(itemNameOnly[^2].ToString(), out unit) && itemNameOnly[^1] == ')')
            {
                isDuplicatedName = true;
            }

            string newName;
            if (isDuplicatedName)
            {
                unit++;
                string baseName = itemNameOnly.Substring(0, itemNameOnly.Length - 3);
                newName = baseName + $"({unit})";
            }
            else
            {
                newName = itemNameOnly + "(1)";
            }

            return newName + extension;
        }

        internal async Task<bool> IsNameAvailable(string siteUrl, RecycleBinItem oRecycleBinItem, string itemNewTitle)
        {
            _appInfo.IsCancelled();

            string itemRelativeUrl = UrlUtility.Combine(oRecycleBinItem.DirName, itemNewTitle);
            _logger.Info(GetType().Name, $"Check if file with name '{itemRelativeUrl}' exists in original location");

            bool exists = true;

            if (oRecycleBinItem.ItemType == RecycleBinItemType.File)
            {
                var oFile = await new SPOFileCSOM(_logger, _appInfo).GetFileAsync(siteUrl, itemRelativeUrl);
                if (oFile.Exists) { exists = false; }
            }
            else if (oRecycleBinItem.ItemType == RecycleBinItemType.Folder)
            {
                var oFolder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(siteUrl, itemRelativeUrl);
                if (oFolder != null) { exists = false; }
            }
            else
            {
                throw new Exception("This is neither a File or a Folder");
            }

            return exists;
        }

        internal async Task RenameTargetLocationFile(string siteUrl, RecycleBinItem oRecycleBinItem, string newName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Check if file with name '{newName}' exists in original location");

            string itemRelativeUrl = UrlUtility.Combine(oRecycleBinItem.DirName, oRecycleBinItem.Title);

            try
            {
                SPOListItemCSOM item = new(_logger, _appInfo);
                if (oRecycleBinItem.ItemType == RecycleBinItemType.File)
                {
                    await new SPOFileCSOM(_logger, _appInfo).RenameFileAsync(siteUrl, itemRelativeUrl, newName);
                }
                else if (oRecycleBinItem.ItemType == RecycleBinItemType.Folder)
                {
                    await new SPOFolderCSOM(_logger, _appInfo).RenameFolderAsync(siteUrl, itemRelativeUrl, newName);
                }
            }
            catch (Exception ex)
            {
                Exception newEx = new($"Error while renaming file '{oRecycleBinItem.Title}' as '{newName}' at '{oRecycleBinItem.DirName}'. {ex.Message}");
                throw newEx;
            }
        }

        private void AddRecord(string siteUrl,
                               RecycleBinItem? oRecycleBinItem = null,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.ItemId = oRecycleBinItem != null ? oRecycleBinItem.Id.ToString() : string.Empty;
            recordItem.ItemTitle = oRecycleBinItem != null ? oRecycleBinItem.Title : String.Empty;
            recordItem.ItemType = oRecycleBinItem != null ? oRecycleBinItem.ItemType.ToString() : String.Empty;
            recordItem.ItemState = oRecycleBinItem != null ? oRecycleBinItem.ItemState.ToString() : String.Empty;

            recordItem.DateDeleted = oRecycleBinItem != null ? oRecycleBinItem.DeletedDate.ToString() : String.Empty;
            recordItem.DeletedByName = oRecycleBinItem != null ? oRecycleBinItem.DeletedByName : String.Empty;
            recordItem.DeletedByEmail = oRecycleBinItem != null ? oRecycleBinItem.DeletedByEmail : String.Empty;

            recordItem.CreatedByName = oRecycleBinItem != null ? oRecycleBinItem.AuthorName : String.Empty;
            recordItem.CreatedByEmail = oRecycleBinItem != null ? oRecycleBinItem.AuthorEmail : String.Empty;
            recordItem.OriginalLocation = oRecycleBinItem != null ? oRecycleBinItem.DirName : String.Empty;

            recordItem.SizeMB = oRecycleBinItem != null ? Math.Round(oRecycleBinItem.Size / Math.Pow(1024, 2), 2).ToString() : String.Empty;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RestoreRecycleBinAutoParameters : ISolutionParameters
    {
        public bool RenameFile { get; set; } = false;
        public SPORecycleBinItemParameters RecycleBinParam { get; set; }
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public RestoreRecycleBinAutoParameters(bool renameFile,
                                               SPORecycleBinItemParameters recycleBinParam,
                                               SPOAdminAccessParameters adminAccess, 
                                               SPOTenantSiteUrlsParameters siteParam)
        {
            RenameFile = renameFile;
            RecycleBinParam = recycleBinParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
