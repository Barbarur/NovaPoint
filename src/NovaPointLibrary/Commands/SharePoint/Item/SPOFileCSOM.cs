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

        internal async Task<string> FindAvailableNameAsync(string siteUrl, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Finding available name");


            string parentFolderPath = fileServerRelativeUrl.Remove(fileServerRelativeUrl.LastIndexOf("/") + 1);

            string potentialName = fileServerRelativeUrl.Substring(fileServerRelativeUrl.LastIndexOf("/") + 1);
            string availableName = string.Empty;

            while (string.IsNullOrWhiteSpace(availableName))
            {
                string potentialNamePath = parentFolderPath + potentialName;
                var oFile = await GetFileAsync(siteUrl, potentialNamePath);
                if (!oFile.Exists)
                {
                    availableName = potentialName;
                }
                else
                {
                    string itemNameOnly = Path.GetFileNameWithoutExtension(potentialName);
                    if (itemNameOnly[^3] == '('
                        && int.TryParse(itemNameOnly[^2].ToString(), out int unit)
                        && itemNameOnly[^1] == ')'
                        && unit >= 9)
                    {
                        throw new Exception($"Too many files with the same name on that location. We couldn't find an available name for the file.");
                    }

                    potentialName = GetNewName(potentialName);
                }
            }

            _logger.LogTxt(GetType().Name, $"File name {availableName} is available.");
            return availableName;
        }

        internal string GetNewName(string itemName)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting new name for item '{itemName}'");

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

    }
}
