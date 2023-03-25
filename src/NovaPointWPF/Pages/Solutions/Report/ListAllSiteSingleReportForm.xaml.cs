using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NovaPointLibrary.Commands.Authentication;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ListAllSiteSingleReportForm.xaml
    /// </summary>
    public partial class ListAllSiteSingleReportForm : Page, ISolutionForm
    {
        // Required parameters for the current report
        public string SiteUrl { get; set; }
        // Optional parameters for the current report to filter lists
        public bool IncludeSystemLists { get; set; }
        public bool IncludeResourceLists { get; set; }

        public ListAllSiteSingleReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SiteUrl = string.Empty;

            IncludeSystemLists = false;
            IncludeResourceLists = false;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            ListAllSiteSingleReportParameters parameters = new(SiteUrl)
            {
                IncludeSystemLists = IncludeSystemLists,
                IncludeResourceLists = IncludeResourceLists
            };
            await new ListAllSiteSingleReport(uiLog, appInfo, parameters).RunAsync();

        }
    }
}
