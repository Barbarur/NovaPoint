using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CopyDuplicateFileAuto
    {
        public readonly static String s_SolutionName = "Copy or Duplicate Files across Sites";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-CopyDuplicateFileAuto";

        private CopyDuplicateFileAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.Hidden,
            l => l.IsSystemList,
            l => l.ParentWeb.Url,

            l => l.BaseType,
            l => l.DefaultViewUrl,
            l => l.Id,
            l => l.ItemCount,
            l => l.Title,


            l => l.RootFolder,
            l => l.RootFolder.ServerRelativeUrl,
        };

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.FileSystemObjectType,
            i => i["SMTotalSize"],

            i => i.Id,
            i => i.File.Name,
            i => i.File.ServerRelativeUrl,
            i => i.File.UIVersionLabel,
            i => i.File.Versions,
            i => i.File.Length,

            i => i.ParentList.RootFolder.ServerRelativeUrl,
        };



        private CopyDuplicateFileAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, CopyDuplicateFileAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(CopyDuplicateFileAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.SourceItemsParam.FileExpresions = _fileExpressions;

            NPLogger logger = new(uiAddLog, "CopyDuplicateFileAuto", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new CopyDuplicateFileAuto(logger, appInfo, parameters).RunScriptAsync();

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

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUserAsync();
            string adminUPN = signedInUser.UserPrincipalName;

            if (_param.AdminAccess.AddAdmin)
            {
                _logger.LogUI(GetType().Name, "Adding Site Collection Admin to source site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.SourceSiteURL, adminUPN);
                _logger.LogUI(GetType().Name, "Adding Site Collection Admin to destination site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.DestinationSiteURL, adminUPN);
            }

            _logger.LogUI(GetType().Name, "Getting source site.");
            var oSourceWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SourceSiteURL);
            _logger.LogUI(GetType().Name, "Getting destination site.");
            var oDestinationWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.DestinationSiteURL);

            if (oSourceWeb.Url == oDestinationWeb.Url)
            {
                _param.SameWebCopyMoveOptimization = true;
            }

            _logger.LogUI(GetType().Name, "Getting source library.");
            var oSourceList = oSourceWeb.GetListByTitle(_param.SourceListTitle, _listExpressions);
            if (oSourceList == null)
            {
                throw new($"Source library '{_param.SourceListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.SourceListTitle}' is not a library.");
            }

            _logger.LogUI(GetType().Name, "Getting destination library.");
            var oDestinationList = oDestinationWeb.GetListByTitle(_param.DestinationListTitle, _listExpressions);
            if (oDestinationList == null)
            {
                throw new($"Destination library '{_param.DestinationListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.DestinationListTitle}' is not a library.");
            }


            string destinationServerRelativeUrl = oDestinationList.RootFolder.ServerRelativeUrl;

            if (!String.IsNullOrWhiteSpace(_param.DestinationFolderServerRelativeUrl))
            {
                var oDestinationFolder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(oDestinationWeb.Url, _param.DestinationFolderServerRelativeUrl);
                if (oDestinationFolder == null)
                {
                    throw new($"Destination folder '{_param.DestinationFolderServerRelativeUrl}' does not exists.");
                }

                destinationServerRelativeUrl = oDestinationFolder.ServerRelativeUrl;
            }


            _logger.LogUI(GetType().Name, "Getting Files from source locaton.");
            List<ListItem> listItemsToMove = new();
            await foreach (var oListItem in new SPOListItemCSOM(_logger, _appInfo).GetAsync(oSourceWeb.Url, oSourceList, _param.SourceItemsParam))
            {
                listItemsToMove.Add(oListItem);
            }


            await CopyMoveListItemsAsync(oSourceWeb, listItemsToMove, destinationServerRelativeUrl);


            if (_param.AdminAccess.RemoveAdmin)
            {
                try
                {
                    _logger.LogUI(GetType().Name, "Adding Site Collection Admin from source site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oSourceWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Site", oSourceWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oSourceWeb.Url}. {ex.Message}";

                    RecordCSV(new(_param, "Failed", remarks: errorMessage));
                }

                try
                {
                    _logger.LogUI(GetType().Name, "Adding Site Collection Admin from destination site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oDestinationWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Site", oDestinationWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oDestinationWeb.Url}. {ex.Message}";

                    RecordCSV(new(_param, "Failed", remarks: errorMessage));
                }
            }

        }

        private async Task CopyMoveListItemsAsync(Web sourceWeb, List<ListItem> listItemsToMove, string destinationServerRelativeUrl)
        {
            var collListItemsByDepth = SegregateItemsByUrlDepth(listItemsToMove);

            _logger.LogUI(GetType().Name, "Coping items...");
            ProgressTracker progress = new(_logger, listItemsToMove.Count);
            foreach (List<ListItem> batchListItemsToMove in collListItemsByDepth)
            {
                await CopyMoveDepthBatchListItems(sourceWeb, batchListItemsToMove, destinationServerRelativeUrl, progress);
            }
        }

        private List<List<ListItem>> SegregateItemsByUrlDepth(List<ListItem> listItemsToMove)
        {
            var dicItemsByUrlDepth = new Dictionary<int, List<ListItem>>();

            foreach (var oListItem in listItemsToMove)
            {
                int depth = GetUrlDepth((string)oListItem["FileRef"]);

                if (!dicItemsByUrlDepth.ContainsKey(depth))
                {
                    dicItemsByUrlDepth[depth] = new List<ListItem>();
                }
                dicItemsByUrlDepth[depth].Add(oListItem);


            _logger.LogUI(GetType().Name, $"Url: {oListItem["FileRef"]} and Depth {depth}");
            }

            return dicItemsByUrlDepth.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
        }

        static int GetUrlDepth(string url)
        {
            return url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private async Task CopyMoveDepthBatchListItems(Web sourceWeb, List<ListItem> listItemsToMove, string destinationServerRelativeUrl, ProgressTracker progress)
        {
            _appInfo.IsCancelled();

            listItemsToMove = listItemsToMove.OrderBy(i => (string)i["FileRef"]).ToList();

            ParallelOptions par = new()
            {
                MaxDegreeOfParallelism = 9,
                CancellationToken = _appInfo.CancelToken,
            };
            await Parallel.ForEachAsync(listItemsToMove, par, async (oListItem, _) =>
            {
                _appInfo.IsCancelled();
                _logger.LogUI(GetType().Name, $"COPY Url: {oListItem["FileRef"]}");

                var itemDestinationServerRelativeUrl = GetItemDestinationServerRelativeUrl(oListItem, destinationServerRelativeUrl);
                _logger.LogUI(GetType().Name, $"DESTINATION IS: {itemDestinationServerRelativeUrl}");
                try
                {
                    await CopyMoveListItem(sourceWeb, oListItem, itemDestinationServerRelativeUrl);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, oListItem.FileSystemObjectType.ToString(), (string)oListItem["FileRef"], ex);

                    RecordCSV(new(_param, "Failed", (string)oListItem["FileRef"], itemDestinationServerRelativeUrl, ex.Message));
                }
                progress.ProgressUpdateReport();
            });

        }

        private string GetItemDestinationServerRelativeUrl(ListItem oListItem, string destinationServerRelativeUrl)
        {
            _logger.LogUI(GetType().Name, $"GET DESTINATION FOR: {destinationServerRelativeUrl}");
            string listItemServerRelativeUrl = (string)oListItem["FileRef"];
            string sourceFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, oListItem.ParentList.RootFolder.ServerRelativeUrl.Length);

            if (!String.IsNullOrWhiteSpace(_param.SourceItemsParam.FolderRelativeUrl))
            {
                sourceFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, _param.SourceItemsParam.FolderRelativeUrl.Length);
            }
            return string.Concat(destinationServerRelativeUrl, sourceFolderRelativeUrl);
        }

        private async Task CopyMoveListItem(Web sourceWeb, ListItem oListItem, string destinationFileServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            string destinationFolderServerRelativeUrl = destinationFileServerRelativeUrl.Remove(destinationFileServerRelativeUrl.LastIndexOf("/"));

            if (!_param.ReportMode)
            {
                await new RESTCopyMoveFileFolder(_logger, _appInfo).CopyMoveAsync(sourceWeb.Url, (string)oListItem["FileRef"], destinationFolderServerRelativeUrl, _param.IsMove, _param.SameWebCopyMoveOptimization);
            }

            RecordCSV(new(_param, "Success", (string)oListItem["FileRef"], destinationFileServerRelativeUrl));
        }

        private void RecordCSV(CopyDuplicateFileAutoRecord record)
        {
            _logger.RecordCSV(record);
        }

    }

    public class CopyDuplicateFileAutoRecord : ISolutionRecord
    {
        internal string SourceSiteURL { get; set; } = String.Empty;
        internal string SourceListTitle { get; set; } = String.Empty;
        internal string SourceItemsServerRelativeUrl { get; set; } = String.Empty;

        internal string DestinationSiteURL { get; set; } = String.Empty;
        internal string DestinationListTitle { get; set; } = String.Empty;
        internal string DestinationItemsServerRelativeUrl { get; set; } = String.Empty;

        internal string Status { get; set; } = String.Empty;
        internal string Remarks { get; set; } = String.Empty;

        internal CopyDuplicateFileAutoRecord(CopyDuplicateFileAutoParameters param,
                                        string status,
                                        string sourceItemsServerRelativeUrl = "",
                                        string destinationItemsServerRelativeUrl = "",
                                        string remarks = "")
        {
            SourceSiteURL = param.SourceSiteURL;
            SourceListTitle = param.SourceListTitle;
            SourceItemsServerRelativeUrl = sourceItemsServerRelativeUrl;

            DestinationSiteURL = param.DestinationSiteURL;
            DestinationListTitle = param.DestinationListTitle;
            DestinationItemsServerRelativeUrl = destinationItemsServerRelativeUrl;

            Status = status;
            Remarks = remarks;
        }

    }


    public class CopyDuplicateFileAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        
        public bool IsMove { get; set; }
        internal bool SameWebCopyMoveOptimization { get; set; } = false;

        public SPOAdminAccessParameters AdminAccess { get; set; }

        private string _sourceSiteURL = string.Empty;
        public string SourceSiteURL
        {
            get { return _sourceSiteURL; }
            set
            {
                _sourceSiteURL = value.Trim();
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
                }
            }
        }
        private string _sourceListTitle = string.Empty;
        public string SourceListTitle
        {
            get { return _sourceListTitle; }
            set { _sourceListTitle = value.Trim(); }
        }
        public SPOItemsParameters SourceItemsParam { get; set; }

        private string _destinationSiteURL = string.Empty;
        public string DestinationSiteURL
        {
            get { return _destinationSiteURL; }
            set
            {
                _destinationSiteURL = value.Trim();
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
                }
            }
        }
        private string _destinationListTitle = string.Empty;
        public string DestinationListTitle
        {
            get { return _destinationListTitle; }
            set { _destinationListTitle = value.Trim(); }
        }
        private string _destinationFolderServerRelativeUrl = String.Empty;
        public string DestinationFolderServerRelativeUrl
        {
            get { return _destinationFolderServerRelativeUrl; }
            set
            {
                _destinationFolderServerRelativeUrl = value.Trim();
                if (!_destinationFolderServerRelativeUrl.StartsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = "/" + _destinationFolderServerRelativeUrl;
                }
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
                }
            }
        }


        public CopyDuplicateFileAutoParameters(
            bool reportMode,
            bool isMove,
            SPOAdminAccessParameters adminAccess,
            string sourceSiteUrl,
            string sourceListTitle,
            SPOItemsParameters sourceItemsParam,
            string targetSiteUrl,
            string targetListTitle,
            string targetFolderServerRelativeUrl)
        {
            ReportMode = reportMode;
            IsMove = isMove;

            AdminAccess = adminAccess;

            SourceSiteURL = sourceSiteUrl;
            SourceListTitle = sourceListTitle;
            SourceItemsParam = sourceItemsParam;
            
            DestinationSiteURL = targetSiteUrl;
            DestinationListTitle = targetListTitle;
            DestinationFolderServerRelativeUrl = targetFolderServerRelativeUrl;
        }

    }
}
