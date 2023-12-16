using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using PnP.Framework.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ItemReportForm.xaml
    /// </summary>
    public partial class ItemReportForm : Page, ISolutionForm
    {
        public string AdminUPN { get; set; }
        public bool RemoveAdmin { get; set; }

        public bool SiteAll { get; set; }
        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public bool ListAll { get; set; }
        public bool IncludeHiddenLists { get; set; }
        public bool IncludeSystemLists { get; set; }
        public string ListTitle { get; set; }


        public bool ItemsAll { get; set; } = false;
        public string FolderRelativeUrl { get; set; } = String.Empty;

        public ItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ItemReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ItemReport);
            SolutionHeader.SolutionDocs = ItemReport.s_SolutionDocs;

            this.AdminUPN = String.Empty;
            this.RemoveAdmin = true;

            this.SiteAll = true;
            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;

            this.ListAll = true;
            this.IncludeHiddenLists = false;
            this.IncludeSystemLists = false;
            this.ListTitle = String.Empty;

            this.ItemsAll = true;
            this.FolderRelativeUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            ItemReportParameters parameters = new()
            {
                AdminUPN = this.AdminUPN,
                RemoveAdmin = this.RemoveAdmin,

                SiteAll = this.SiteAll,
                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,

                ListAll = this.ListAll,
                IncludeHiddenLists = this.IncludeHiddenLists,
                IncludeSystemLists = this.IncludeSystemLists,
                ListTitle = this.ListTitle,

                ItemsAll = this.ItemsAll,
                FolderRelativeUrl = this.FolderRelativeUrl,
            };

            await new ItemReport(appInfo, uiLog, parameters).RunAsync();

        }

    }
}
