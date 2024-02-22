using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using File = Microsoft.SharePoint.Client.File;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOFileCSOM
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOFileCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<File> GetFileAsync(string siteUrl, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}";
            _logger.LogTxt(methodName, $"Start getting Item '{fileServerRelativeUrl}' from '{siteUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            return GetFileAsync(clientContext, fileServerRelativeUrl);
        }

        private File GetFileAsync(ClientContext clientContext, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            if (!fileServerRelativeUrl.StartsWith("/"))
            {
                fileServerRelativeUrl = fileServerRelativeUrl.Insert(0, "/");
            }

            var defaultExpressions = new Expression<Func<File, object>>[]
            {
                f => f.Exists,
                f => f.Name,
                f => f.ServerRelativePath,
                f => f.ServerRelativeUrl,
            };

            File oFile = clientContext.Web.GetFileByServerRelativeUrl(fileServerRelativeUrl);
            clientContext.Load(oFile, defaultExpressions);
            clientContext.ExecuteQueryRetry();

            return oFile;
        }

        internal async Task RenameFileAsync(string siteUrl, string fileServerRelativeUrl, string newName)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start renaming file '{fileServerRelativeUrl}' to '{newName}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            File oFile = GetFileAsync(clientContext, fileServerRelativeUrl);
            
            var targetPath = string.Concat(oFile.ServerRelativePath.DecodedUrl.Remove(oFile.ServerRelativePath.DecodedUrl.Length - oFile.Name.Length), newName);
            oFile.MoveToUsingPath(ResourcePath.FromDecodedUrl(targetPath), MoveOperations.None);
            clientContext.ExecuteQueryRetry();
        }

        internal async Task CheckInAsync(string siteUrl, ListItem oFile, CheckinType checkinType, string comment)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Check-in file '{oFile["FileRef"]}' at '{siteUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.CheckInFile($"{oFile["FileRef"]}", checkinType, comment);

        }
    }
}
