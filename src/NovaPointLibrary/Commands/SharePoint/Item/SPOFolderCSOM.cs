using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOFolderCSOM
    {
        private readonly ILogger _logger;
        private readonly IAppClient _appInfo;

        internal SPOFolderCSOM(ILogger logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Folder?> GetFolderAsync(string siteUrl, string folderServerRelativeUrl, Expression<Func<Folder, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start getting Folder '{folderServerRelativeUrl}' from '{siteUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            return GetFolderAsync(clientContext, folderServerRelativeUrl, retrievalExpressions);
        }

        internal Folder? GetFolderAsync(ClientContext clientContext, string folderServerRelativeUrl, Expression<Func<Folder, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();

            if (!folderServerRelativeUrl.StartsWith("/"))
            {
                folderServerRelativeUrl = folderServerRelativeUrl.Insert(0, "/");
            }

            var defaultExpressions = new Expression<Func<Folder, object>>[]
            {
                f => f.Exists,
                f => f.Name,
                f => f.ServerRelativePath,
                f => f.ServerRelativeUrl,
            };
            if (retrievalExpressions != null) { defaultExpressions = retrievalExpressions.Union(defaultExpressions).ToArray(); }

            try
            {
                Folder oFolder = clientContext.Web.GetFolderByServerRelativeUrl(folderServerRelativeUrl);
                clientContext.Load(oFolder, defaultExpressions);
                clientContext.ExecuteQueryRetry();

                return oFolder;
            }
            catch
            {
                _logger.Info(GetType().Name, $"Folder '{folderServerRelativeUrl}' doesn't exists.");
                return null;
            }
        }

        internal async Task RenameFolderAsync(string siteUrl, string fileServerRelativeUrl, string newName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start renaming folder '{fileServerRelativeUrl}' to '{newName}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            Folder? oFolder = GetFolderAsync(clientContext, fileServerRelativeUrl);

            if(oFolder != null)
            {
                var targetPath = string.Concat(oFolder.ServerRelativePath.DecodedUrl.Remove(oFolder.ServerRelativePath.DecodedUrl.Length - oFolder.Name.Length), newName);
                oFolder.MoveToUsingPath(ResourcePath.FromDecodedUrl(targetPath));
                clientContext.ExecuteQueryRetry();

            }
            else
            {
                throw new Exception($"Folder '{fileServerRelativeUrl}' doesn't exists.");
            }
        }

        internal async Task CreateAsync(string siteUrl, string folderServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            if (!folderServerRelativeUrl.StartsWith("/"))
            {
                folderServerRelativeUrl = folderServerRelativeUrl.Insert(0, "/");
            }

            _logger.Info(GetType().Name, $"Creating folder '{folderServerRelativeUrl}' on site {siteUrl}");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            clientContext.Web.Folders.Add(folderServerRelativeUrl);
            clientContext.ExecuteQueryRetry();
        }


        internal async Task EnsureFolderPathExistAsync(string siteUrl, string folderServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Ensuring folder path exists '{folderServerRelativeUrl}'");

            var folder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(siteUrl, folderServerRelativeUrl);

            if (folder == null)
            {
                string parentPath = folderServerRelativeUrl.Remove(folderServerRelativeUrl.LastIndexOf("/"));
                await EnsureFolderPathExistAsync(siteUrl, parentPath);

                await new SPOFolderCSOM(_logger, _appInfo).CreateAsync(siteUrl, folderServerRelativeUrl);
            }
        }

        internal async Task<RESTStorageMetricsResponse> GetFolderStorageMetricAsync(string siteUrl, Folder folder)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting storage metrics from folder '{folder.ServerRelativeUrl}' from '{siteUrl}'");

            string api = siteUrl + $"/_api/Web/GetFolderByServerRelativeUrl('{folder.ServerRelativeUrl}')?&$select=StorageMetrics&$expand=StorageMetrics";

            var response = await new RESTAPIHandler(_logger, _appInfo).GetAsync(api);

            var storageMetricsResponse = JsonConvert.DeserializeObject<RESTStorageMetricsResponse>(response);

            return storageMetricsResponse;
        }

    }
}
