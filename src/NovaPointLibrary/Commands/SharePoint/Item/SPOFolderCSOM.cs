using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOFolderCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOFolderCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Folder?> GetFolderAsync(string siteUrl, string folderServerRelativeUrl, Expression<Func<Folder, object>>[]? retrievalExpressions = null)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start getting Item '{folderServerRelativeUrl}' from '{siteUrl}'");

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
                _logger.LogTxt(GetType().Name, $"Folder '{folderServerRelativeUrl}' doesn't exists.");
                return null;
            }
        }

        internal async Task RenameFolderAsync(string siteUrl, string fileServerRelativeUrl, string newName)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start renaming folder '{fileServerRelativeUrl}' to '{newName}'");

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
            _logger.LogTxt(GetType().Name, $"Creating folder '{folderServerRelativeUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);
            clientContext.Web.Folders.Add(folderServerRelativeUrl);
            clientContext.ExecuteQueryRetry();
        }
    }
}
