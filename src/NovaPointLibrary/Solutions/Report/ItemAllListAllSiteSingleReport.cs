using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Commands.List;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    public class ItemAllListAllSiteSingleReport
    {
        // Baic parameters required for all reports
        private readonly LogHelper _logHelper;
        private readonly AppInfo AppInfo;
        // Required parameters for the current report
        private readonly string SiteUrl;
        // Optional parameters for the current report
        private readonly bool IncludeSystemLists = false;
        private readonly bool IncludeResourceLists = false;

        public ItemAllListAllSiteSingleReport(Action<LogInfo> uiAddLog, AppInfo appInfo, ItemAllListAllSiteSingleReportParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            AppInfo = appInfo;
            SiteUrl = parameters.SiteUrl;
            IncludeSystemLists = parameters.IncludeSystemLists;
            IncludeResourceLists = parameters.IncludeResourceLists;
        }
        public async Task RunAsync()
        {
            try
            {
                if ( String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl) )
                {
                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Collection Administrators";
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
            string rootSiteAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

            double counterList = 0;
            if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };
            List<List> collList = new GetList(_logHelper, rootSiteAccessToken).CSOM_All(SiteUrl, IncludeSystemLists, IncludeResourceLists);
            foreach (List oList in collList)
            {
                if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                double progress = Math.Round(counterList * 100 / collList.Count, 2);
                counterList++;
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing List '{oList.Title}'");


                double counterItem = 0;
                List<ListItem> collItems = new GetItem(_logHelper, rootSiteAccessToken).CsomAllItems(SiteUrl, oList.Title);
                foreach (ListItem oItem in collItems)
                {
                    if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                    double subprogress = Math.Round(counterItem * (100 / collList.Count / collItems.Count) + progress, 2);

                    counterItem++;
                    _logHelper.AddProgressToUI(subprogress);

                    dynamic recordItem = new ExpandoObject();
                    recordItem.SiteUrl = SiteUrl;
                    recordItem.Title = oList.Title;
                    recordItem.LibraryType = oList.BaseType;

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

                    if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                    if (oList.BaseType.ToString() == "DocumentLibrary")
                    {

                        float fileSizeMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                        recordItem.FileSizeMB = fileSizeMb;

                        FieldLookupValue fvFileSizeTotalMb = (FieldLookupValue)oItem["SMTotalSize"];
                        float fileSizeTotalMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(fvFileSizeTotalMb.LookupId / Math.Pow(1024, 2), 2);
                        recordItem.FileSizeTotalMB = fileSizeTotalMb;

                    }
                    else if (oList.BaseType.ToString() == "GenericList")
                    {

                        recordItem.FileSizeMB = $"Size for items is not yet supported";
                        recordItem.FileSizeTotalMB = $"Size for items is not yet supported";

                    }

                    _logHelper.AddRecordToCSV(recordItem);

                }

            }

            _logHelper.ScriptFinishSuccessfulNotice();

        }

    }


    public class ItemAllListAllSiteSingleReportParameters
    {
        // Required parameters for the current report
        internal string SiteUrl;
        public bool IncludeSystemLists { get; set; } = false;
        public bool IncludeResourceLists { get; set; } = false;

        public ItemAllListAllSiteSingleReportParameters(string siteUrl)
        {
            SiteUrl = siteUrl;
        }
    }
}
