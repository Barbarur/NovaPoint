using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Commands.List;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    // TO BE DEPRECATED ONCE ItemReport IS ON PRODUCTION
    public class ItemAllListSingleSiteSingleReport
    {
        public static string _solutionName = "Report of all Files/Items in a Library/List of a Site";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemAllListSingleSiteSingleReport";

        private readonly LogHelper _logHelper;
        private readonly AppInfo AppInfo;
        // Required parameters for the current report
        private readonly string SiteUrl;
        private readonly string Listname;

        public ItemAllListSingleSiteSingleReport(Action<LogInfo> uiAddLog, AppInfo appInfo, ItemAllListSingleSiteSingleReporParameters parameters)
        {
            // Baic parameters required for all reports
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            AppInfo = appInfo;
            // Required parameters for the current report
            SiteUrl = parameters.SiteUrl;
            Listname = parameters.ListName;
        }
        public async Task RunAsync()
        {
            try
            {
                if ( String.IsNullOrWhiteSpace(SiteUrl) || String.IsNullOrWhiteSpace(Listname))
                {
                    string message = $"FORM INCOMPLETED: Please fill up the form";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsync();
                }

            }
            catch (Exception ex)
            {
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            
            _logHelper.ScriptStartNotice();

            string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
            string accessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

            if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };
            List? list = new GetList(_logHelper, accessToken).CSOM_Single(SiteUrl, Listname);

            if (list == null)
            {
                string message = $"ERROR: List '{Listname}' no found in Site '{SiteUrl}'";
                Exception ex = new(message);
                throw ex;
            }

            if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };
            List<ListItem> collItems = new GetItem(_logHelper, accessToken).CsomAllItems(SiteUrl, Listname);
            foreach (ListItem oItem in collItems)
            {
                if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                dynamic recordItem = new ExpandoObject();
                recordItem.SiteUrl = SiteUrl;
                recordItem.LibraryName = Listname;

                recordItem.ItemID = oItem["ID"];
                recordItem.ItemName = oItem["FileLeafRef"];
                recordItem.ItemPath = oItem["FileRef"];
                recordItem.Type = oItem.FileSystemObjectType;

                recordItem.Created = oItem["Created"];
                FieldUserValue author = (FieldUserValue)oItem["Author"];
                recordItem.CreatedBy = author.Email;

                recordItem.Modified = oItem["Modified"];
                FieldUserValue editor = (FieldUserValue)oItem["Editor"];
                recordItem.ModifiedBy = editor.Email;

                if (list.BaseType.ToString() == "DocumentLibrary")
                {
                    if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                    float fileSizeMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                    recordItem.FileSizeMB = fileSizeMb;

                    FieldLookupValue fvFileSizeTotalMb = (FieldLookupValue)oItem["SMTotalSize"];
                    float fileSizeTotalMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(fvFileSizeTotalMb.LookupId / Math.Pow(1024, 2), 2);
                    recordItem.FileSizeTotalMB = fileSizeTotalMb;

                }
                if (list.BaseType.ToString() == "GenericList")
                {
                    if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); }; 
                    
                    recordItem.FileSizeMB = $"Size for items is not yet supported";
                }

                _logHelper.AddRecordToCSV(recordItem);

            }

            _logHelper.ScriptFinishSuccessfulNotice();

        }

    }


    public class ItemAllListSingleSiteSingleReporParameters
    {
        // Required parameters for the current report
        internal string SiteUrl;
        internal string ListName;

        public ItemAllListSingleSiteSingleReporParameters ( string siteUrl, string listName)
        {
            SiteUrl = siteUrl;
            ListName = listName;
        }
    }
}
