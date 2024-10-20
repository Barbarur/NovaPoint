using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CopyFilesAuto
    {
        public readonly static String s_SolutionName = "Copy Files across sites";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-CopyFilesAuto";

        private CopyFilesAutoParameters _param;
        private readonly LoggerSolution _logger;
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



        private CopyFilesAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, CopyFilesAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(CopyFilesAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.SourceItemsParam.FileExpresions = _fileExpressions;

            LoggerSolution logger = new(uiAddLog, "CopyFilesAuto", parameters);
            //return;
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new CopyFilesAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUserAsync();
            string adminUPN = signedInUser.UserPrincipalName;

            if (_param.AdminAccess.AddAdmin)
            {
                _logger.UI(GetType().Name, "Adding Site Collection Admin to source site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.SourceSiteURL, adminUPN);
                _logger.UI(GetType().Name, "Adding Site Collection Admin to destination site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.DestinationSiteURL, adminUPN);
            }

            _logger.UI(GetType().Name, "Getting source site.");
            var oSourceWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SourceSiteURL);
            _logger.UI(GetType().Name, "Getting destination site.");
            var oDestinationWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.DestinationSiteURL);

            _logger.UI(GetType().Name, "Getting source library.");
            var oSourceList = oSourceWeb.GetListByTitle(_param.SourceListTitle, _listExpressions);
            if (oSourceList == null)
            {
                throw new($"Source library '{_param.SourceListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.SourceListTitle}' is not a library.");
            }

            _logger.UI(GetType().Name, "Getting destination library.");
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


            _logger.UI(GetType().Name, "Getting Files from source locaton.");
            List<ListItem> listFilesToCopy = new();
            await foreach (var oListItem in new SPOListItemCSOM(_logger, _appInfo).GetAsync(oSourceWeb.Url, oSourceList, _param.SourceItemsParam))
            {
                if (oListItem.FileSystemObjectType.ToString() == "Folder") { continue; }
                listFilesToCopy.Add(oListItem);
            }


            // This sorting is not needed, but another might bring better structure during migration.
            listFilesToCopy = listFilesToCopy.OrderBy(i => i.File.ServerRelativeUrl).ToList();


            _logger.UI(GetType().Name, "Coping files...");
            ProgressTracker progress = new(_logger, listFilesToCopy.Count);
            var par = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            await Parallel.ForEachAsync(listFilesToCopy, par, async (listItem, _) =>
            {
                await ProcessListItemsAsync(oSourceWeb, oDestinationWeb, listItem, destinationServerRelativeUrl);
                progress.ProgressUpdateReport();
            });

            if (_param.AdminAccess.RemoveAdmin)
            {
                try
                {
                    _logger.UI(GetType().Name, "Adding Site Collection Admin from source site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oSourceWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", oSourceWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oSourceWeb.Url}. {ex.Message}";
                    CopyLibraryFilesRecord record = new(_param, "Error", errorMessage);
                    RecordCSV(record);
                }

                try
                {
                    _logger.UI(GetType().Name, "Adding Site Collection Admin from destination site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oDestinationWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", oDestinationWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oDestinationWeb.Url}. {ex.Message}";
                    CopyLibraryFilesRecord record = new(_param, "Error", errorMessage);
                    RecordCSV(record);
                }
            }

        }

        private async Task ProcessListItemsAsync(Web sourceWeb, Web destinationWeb, ListItem oListItem, string destinationServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            string sourceFolderRelativeUrl = oListItem.File.ServerRelativeUrl.Remove(0, oListItem.ParentList.RootFolder.ServerRelativeUrl.Length);

            if (!String.IsNullOrWhiteSpace(_param.SourceItemsParam.FolderRelativeUrl))
            {
                sourceFolderRelativeUrl = oListItem.File.ServerRelativeUrl.Remove(0, _param.SourceItemsParam.FolderRelativeUrl.Length);
            }
            string destinationFileServerRelativeUrl = string.Concat(destinationServerRelativeUrl, sourceFolderRelativeUrl);

            try
            {
                await ProcessFile(sourceWeb, destinationWeb, oListItem, destinationFileServerRelativeUrl);

            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "File", oListItem.File.ServerRelativeUrl, ex);
                CopyLibraryFilesRecord record = new(_param, "Error", oListItem.File.ServerRelativeUrl, destinationFileServerRelativeUrl, ex.Message);
                RecordCSV(record);
            }

        }

        private async Task ProcessFile(Web sourceWeb, Web destinationWeb, ListItem oListItem, string destinationFileServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            var oFile = await new SPOFileCSOM(_logger, _appInfo).GetFileAsync(destinationWeb.Url, destinationFileServerRelativeUrl);

            if (oFile.Exists)
            {
                string errorMessage = "File with the same name already exist at the destination folder";
                _logger.Error(GetType().Name, "File", oListItem.File.ServerRelativeUrl, new(errorMessage));
                CopyLibraryFilesRecord record = new(_param, "Error", oListItem.File.ServerRelativeUrl, destinationFileServerRelativeUrl, errorMessage);
                RecordCSV(record);
            }
            else
            {
                try
                {
                    string destinationFolderServerRelativeUrl = destinationFileServerRelativeUrl.Remove(destinationFileServerRelativeUrl.LastIndexOf("/"));

                    if (!_param.ReportMode)
                    {
                        await new SPOFolderCSOM(_logger, _appInfo).EnsureFolderPathExistAsync(destinationWeb.Url, destinationFolderServerRelativeUrl);

                        await new SPOFileCSOM(_logger, _appInfo).CopyAsync(sourceWeb.Url, oListItem.File.ServerRelativeUrl, destinationFolderServerRelativeUrl, false);
                    }
                    CopyLibraryFilesRecord record = new(_param, "Copied", oListItem.File.ServerRelativeUrl, destinationFileServerRelativeUrl);
                    RecordCSV(record);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "File", oListItem.File.ServerRelativeUrl, ex);
                    CopyLibraryFilesRecord record = new(_param, "Failed", oListItem.File.ServerRelativeUrl, destinationFileServerRelativeUrl, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private void RecordCSV(CopyLibraryFilesRecord record)
        {
            _logger.RecordCSV(record);
        }

    }

    public class CopyLibraryFilesRecord : ISolutionRecord
    {
        internal string SourceSiteURL { get; set; } = String.Empty;
        internal string SourceListTitle { get; set; } = String.Empty;
        internal string SourceItemsServerRelativeUrl { get; set; } = String.Empty;

        internal string DestinationSiteURL { get; set; } = String.Empty;
        internal string DestinationListTitle { get; set; } = String.Empty;
        internal string DestinationItemsServerRelativeUrl { get; set; } = String.Empty;

        internal string Status {  get; set; } = String.Empty;
        internal string Remarks { get; set; } = String.Empty;

        internal CopyLibraryFilesRecord(CopyFilesAutoParameters param,
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


    public class CopyFilesAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true; 
        
        public SPOAdminAccessParameters AdminAccess {  get; set; }

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

        public CopyFilesAutoParameters(bool reportMode, SPOAdminAccessParameters adminAccess,
                                       string sourceSiteUrl, string sourceListTitle, SPOItemsParameters sourceItemsParam,
                                       string targetSiteUrl, string targetListTitle, string targetFolderServerRelativeUrl)
        {
            ReportMode = reportMode;

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
