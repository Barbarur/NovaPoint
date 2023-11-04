//using Microsoft.SharePoint.Client;
//using Newtonsoft.Json;
//using NovaPointLibrary.Solutions.Reports;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using NovaPointLibrary.Commands.Authentication;

//namespace NovaPointWPF.Pages.Solutions.Report
//{
//    /// <summary>
//    /// Interaction logic for ItemAllListSingleSiteSingleReportForm.xaml
//    /// </summary>
//    public partial class ItemAllListSingleSiteSingleReportForm : Page, ISolutionForm
//    {
//        // Required parameters for the current report
//        public string SiteUrl { get; set; }
//        public string ListName { get; set; }
        
//        public ItemAllListSingleSiteSingleReportForm()
//        {
//            InitializeComponent();

//            DataContext = this;

//            SiteUrl = string.Empty;
//            ListName = string.Empty;
//        }

//        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
//        {

//            ItemAllListSingleSiteSingleReporParameters parameters = new(SiteUrl, ListName);
//            await new ItemAllListSingleSiteSingleReport(uiLog, appInfo, parameters).RunAsync();

//        }
//    }
//}
