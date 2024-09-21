using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RestorePHLItemAuto
    {
        public static readonly string s_SolutionName = "Restore Files from Preservation";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RestorePHLItemAuto";

        private RestorePHLItemAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            f => f.HasUniqueRoleAssignments,
            f => f["Author"],
            f => f["Created"],
            f => f["Editor"],
            f => f["ID"],
            f => f.File.CheckOutType,
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
            f => f["File_x0020_Size"],
            f => f["Modified"],
            f => f["PreservationDatePreserved"],
            f => f["PreservationOriginalDocumentName"],
            f => f["PreservationOriginalListId"],
            f => f["PreservationOriginalURL"],
            f => f["SMTotalSize"],
            f => f.Versions,
            f => f["_UIVersionString"],

        };

        private RestorePHLItemAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, RestorePHLItemAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RestorePHLItemAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            //parameters.TListsParam.SiteAccParam.SiteParam.IncludeSubsites = false;
            parameters.ListsParam.AllLists = false;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeLibraries = false;
            parameters.ListsParam.ListTitle = "Preservation Hold Library";
            parameters.ItemsParam.FileExpresions = _fileExpressions;

            NPLogger logger = new(uiAddLog, "RestorePHLItemAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RestorePHLItemAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }


        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();



            await foreach (var result in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (result.Ex != null)
                {
                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, remarks: result.Ex.Message);
                    continue;
                }

                if (result.Item == null || result.ListRecord.List == null) { continue; }

                try
                {
                    ClientContext clientContext = await _appInfo.GetContext(result.ListRecord.SiteUrl);

                    if (result.Item.FileSystemObjectType.ToString() == "Folder") { continue; }

                    if (!_param.PHLParam.MatchParameters(result.Item))
                    {
                        continue;
                    }

                    var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.File, object>>[]
                    {
                        f => f.Exists,
                        f => f.Name,
                        f => f.ServerRelativePath,
                        f => f.ServerRelativeUrl,
                    };
                    Microsoft.SharePoint.Client.File oFile = clientContext.Web.GetFileByServerRelativeUrl((string)result.Item["FileRef"]);
                    clientContext.Load(oFile, defaultExpressions);
                    clientContext.ExecuteQueryRetry();

                    string targetFilePath;
                    if (_param.RestoreOriginalLocation)
                    {
                        var oList = clientContext.Web.GetListById(Guid.Parse((string)result.Item["PreservationOriginalListId"]));
                        if (oList.BaseType == BaseType.GenericList)
                        {
                            string remarks = "This was originally a List Item. Items cannot be restored";
                            AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, remarks: remarks);
                            continue;
                        }

                        if (oList == null)
                        {
                            string remarks = "Original Library does not exist. File cannot be restored";
                            AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, remarks: remarks);
                            continue;
                        }

                        string originalPath = (string)result.Item["PreservationOriginalURL"];
                        string targetFolderPath = originalPath.Remove(originalPath.LastIndexOf("/"));
                        await EnsureFolderPathExist(result.ListRecord.SiteUrl, targetFolderPath);

                        string itemNameOnly = Path.GetFileNameWithoutExtension((string)result.Item["PreservationOriginalDocumentName"]);
                        var extension = Path.GetExtension((string)result.Item["FileLeafRef"]);
                        targetFilePath = targetFolderPath + "/" + itemNameOnly + "_restored" + extension;

                        string newName = await new SPOFileCSOM(_logger, _appInfo).FindAvailableNameAsync(result.ListRecord.SiteUrl, targetFilePath);
                        targetFilePath = targetFolderPath + "/" + newName;
                        oFile.CopyToUsingPath(ResourcePath.FromDecodedUrl(targetFilePath), false);
                        clientContext.ExecuteQueryRetry();
                    }
                    else
                    {
                        string itemNameOnly = Path.GetFileNameWithoutExtension((string)result.Item["PreservationOriginalDocumentName"]);
                        var extension = Path.GetExtension((string)result.Item["FileLeafRef"]);
                        targetFilePath = _param.RestoreTargetLocation + "/" + itemNameOnly + "_restored" + extension;

                        string newName = await new SPOFileCSOM(_logger, _appInfo).FindAvailableNameAsync(result.ListRecord.SiteUrl, targetFilePath);
                        targetFilePath = _param.RestoreTargetLocation + "/" + newName;
                        oFile.CopyToUsingPath(ResourcePath.FromDecodedUrl(targetFilePath), false);
                        clientContext.ExecuteQueryRetry();

                    }

                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, targetFilePath);

                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Item", (string)result.Item["FileRef"], ex);

                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, remarks: ex.Message);
                }
            }
        }

        private async Task EnsureFolderPathExist(string siteUrl, string folderPath)
        {
            _logger.LogTxt(GetType().Name, $"Check folder path {folderPath}");

            var folder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(siteUrl, folderPath);

            if (folder == null)
            {
                string parentPath = folderPath.Remove(folderPath.LastIndexOf("/"));
                await EnsureFolderPathExist(siteUrl, parentPath);

                await new SPOFolderCSOM(_logger, _appInfo).CreateAsync(siteUrl, folderPath);
            }
        }

        private async Task IstargetLocationValid()
        {
            if (_param.RestoreOriginalLocation) { return; }
            else
            {
                var folder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(_param.SiteParam.SiteUrl, _param.RestoreTargetLocation);
                if (folder == null) { throw new Exception("Target location does not exist."); }
            }
        }


        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string remarks = "")
        {

            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListDefaultViewUrl = oList != null ? oList.DefaultViewUrl : string.Empty;

            recordItem.ListGUID = oItem != null ? oItem["PreservationOriginalListId"] : string.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;

            recordItem.ItemName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
            recordItem.ItemOriginalName = oItem != null ? oItem["PreservationOriginalDocumentName"] : string.Empty;

            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;
            recordItem.ItemOriginalPath = oItem != null ? oItem["PreservationOriginalURL"] : string.Empty;

            recordItem.ItemPreserved = oItem != null ? oItem["PreservationDatePreserved"] : string.Empty;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty;


            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RestorePHLItemAutoParameters : ISolutionParameters
    {
        public bool RestoreOriginalLocation { get; set; }

        private string _restoreTargetLocation = string.Empty;
        public string RestoreTargetLocation
        {
            get { return _restoreTargetLocation; }
            init { _restoreTargetLocation = value.Trim().TrimEnd('/'); }
        }
        public SPOPreservationHoldLibraryParameters PHLParam { get; set; }
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        internal readonly SPOListsParameters ListsParam = new();
        internal readonly SPOItemsParameters ItemsParam = new();
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public RestorePHLItemAutoParameters(bool restoreOriginalLocation,
                                            string restoreTargetLocation,
                                            SPOPreservationHoldLibraryParameters phlParam,
                                            SPOAdminAccessParameters adminAccess,
                                            SPOTenantSiteUrlsParameters siteParam)
        {
            RestoreOriginalLocation = restoreOriginalLocation;
            RestoreTargetLocation = restoreTargetLocation;
            PHLParam = phlParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }

        public void ParametersCheck()
        {
            if (!RestoreOriginalLocation && string.IsNullOrEmpty(RestoreTargetLocation))
            {
                throw new Exception("Target location server relative url cannot be empty");
            }
            if (!RestoreOriginalLocation && string.IsNullOrWhiteSpace(SiteParam.SiteUrl))
            {
                throw new Exception("Restoring on a specific location is only supported when restoring a single site");
            }
        }
    }
}
