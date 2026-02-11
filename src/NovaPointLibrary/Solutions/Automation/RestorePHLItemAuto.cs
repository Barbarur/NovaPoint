using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RestorePHLItemAuto : ISolution
    {
        public static readonly string s_SolutionName = "Restore Files from Preservation";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RestorePHLItemAuto";

        private ContextSolution _ctx;
        private RestorePHLItemAutoParameters _param;

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

        private RestorePHLItemAuto(ContextSolution context, RestorePHLItemAutoParameters parameters)
        {
            _ctx = context;

            parameters.ListsParam.AllLists = false;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeLibraries = false;
            parameters.ListsParam.ListTitle = "Preservation Hold Library";
            parameters.ItemsParam.FileExpressions = _fileExpressions;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RestorePHLItemAuto(context, (RestorePHLItemAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();



            await foreach (var result in new SPOTenantItemsCSOM(_ctx.Logger, _ctx.AppClient, _param.TItemsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (result.Ex != null)
                {
                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, remarks: result.Ex.Message);
                    continue;
                }

                if (result.Item == null || result.ListRecord.List == null) { continue; }

                try
                {
                    ClientContext clientContext = await _ctx.AppClient.GetContext(result.ListRecord.SiteUrl);

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

                        string newName = await new SPOFileCSOM(_ctx.Logger, _ctx.AppClient).FindAvailableNameAsync(result.ListRecord.SiteUrl, targetFilePath);
                        targetFilePath = targetFolderPath + "/" + newName;
                        oFile.CopyToUsingPath(ResourcePath.FromDecodedUrl(targetFilePath), false);
                        clientContext.ExecuteQueryRetry();
                    }
                    else
                    {
                        string itemNameOnly = Path.GetFileNameWithoutExtension((string)result.Item["PreservationOriginalDocumentName"]);
                        var extension = Path.GetExtension((string)result.Item["FileLeafRef"]);
                        targetFilePath = _param.RestoreTargetLocation + "/" + itemNameOnly + "_restored" + extension;

                        string newName = await new SPOFileCSOM(_ctx.Logger, _ctx.AppClient).FindAvailableNameAsync(result.ListRecord.SiteUrl, targetFilePath);
                        targetFilePath = _param.RestoreTargetLocation + "/" + newName;
                        oFile.CopyToUsingPath(ResourcePath.FromDecodedUrl(targetFilePath), false);
                        clientContext.ExecuteQueryRetry();

                    }

                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, targetFilePath);

                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Item", (string)result.Item["FileRef"], ex);

                    AddRecord(result.ListRecord.SiteUrl, result.ListRecord.List, result.Item, remarks: ex.Message);
                }
            }
        }

        private async Task EnsureFolderPathExist(string siteUrl, string folderPath)
        {
            _ctx.Logger.Info(GetType().Name, $"Check folder path {folderPath}");

            var folder = await new SPOFolderCSOM(_ctx.Logger, _ctx.AppClient).GetFolderAsync(siteUrl, folderPath);

            if (folder == null)
            {
                string parentPath = folderPath.Remove(folderPath.LastIndexOf("/"));
                await EnsureFolderPathExist(siteUrl, parentPath);

                await new SPOFolderCSOM(_ctx.Logger, _ctx.AppClient).CreateAsync(siteUrl, folderPath);
            }
        }

        private async Task IstargetLocationValid()
        {
            if (_param.RestoreOriginalLocation) { return; }
            else
            {
                var folder = await new SPOFolderCSOM(_ctx.Logger, _ctx.AppClient).GetFolderAsync(_param.SiteParam.SiteUrl, _param.RestoreTargetLocation);
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

            _ctx.Logger.DynamicCSV(recordItem);
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
