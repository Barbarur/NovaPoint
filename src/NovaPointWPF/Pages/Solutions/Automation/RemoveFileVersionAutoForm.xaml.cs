using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveFileVersionAutoForm.xaml
    /// </summary>
    public partial class RemoveFileVersionAutoForm : Page, ISolutionForm
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


        public bool ItemsAll { get; set; }
        public string FolderRelativeUrl { get; set; }

        public bool DeleteAll { get; set; }
        public int VersionsToKeep { get; set; }
        public bool Recycle { get; set; }

        public bool ReportMode { get; set; }


        public RemoveFileVersionAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveFileVersionAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveFileVersionAuto);
            SolutionHeader.SolutionDocs = RemoveFileVersionAuto.s_SolutionDocs;

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

            this.DeleteAll = false;
            this.KeepVersions = true;
            this.VersionsToKeep = 500;
            this.Recycle = true;

            this.ReportMode = true;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            RemoveFileVersionAutoParameters parameters = new()

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

                FolderRelativeUrl = this.FolderRelativeUrl,

                DeleteAll = this.DeleteAll,
                VersionsToKeep = this.VersionsToKeep,
                Recycle = this.Recycle,

                ReportMode = this.ReportMode,
            };

            await new RemoveFileVersionAuto(parameters, uiLog, cancelTokenSource).RunAsync();
        }

        private bool _keepVersions;
        public bool KeepVersions
        {
            get { return _keepVersions; }
            set
            {
                _keepVersions = value;
                if (value) { KeepVersionsForm.Visibility = Visibility.Visible; }
                else { KeepVersionsForm.Visibility = Visibility.Collapsed; }
            }
        }
    }
}
