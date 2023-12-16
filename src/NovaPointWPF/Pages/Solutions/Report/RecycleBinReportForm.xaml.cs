using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for RecycleBinReportForm.xaml
    /// </summary>
    public partial class RecycleBinReportForm : Page, ISolutionForm
    {
        public string AdminUPN { get; set; }
        public bool RemoveAdmin { get; set; }

        public bool SiteAll { get; set; }
        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public bool FirstStage { get; set; }
        public bool SecondStage { get; set; }
        public DateTime DeletedAfter { get; set; }
        public DateTime DeletedBefore { get; set; }
        public string DeletedByEmail { get; set; }
        public string OriginalLocation { get; set; }
        public double FileSizeMb { get; set; }
        public bool FileSizeAbove { get; set; }

        public RecycleBinReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RecycleBinReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RecycleBinReport);
            SolutionHeader.SolutionDocs = RecycleBinReport.s_SolutionDocs;

            this.AdminUPN = String.Empty;
            this.RemoveAdmin = true;

            this.SiteAll = true;
            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;

            this.FirstStage = true;
            this.SecondStage = true;
            this.DeletedAfter = DateTime.UtcNow.AddDays(-94);
            this.DeletedBefore = DateTime.UtcNow.AddDays(1);
            this.DeletedByEmail = string.Empty;
            this.OriginalLocation = string.Empty;
            this.FileSizeMb = 0;
            this.FileSizeAbove = true;

        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            RecycleBinReportParameters parameters = new()
            {
                AdminUPN = this.AdminUPN,
                RemoveAdmin = this.RemoveAdmin,

                SiteAll = this.SiteAll,
                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,

                FirstStage = this.FirstStage,
                SecondStage = this.SecondStage,
                DeletedAfter = this.DeletedAfter,
                DeletedBefore = this.DeletedBefore,
                DeletedByEmail = this.DeletedByEmail,
                OriginalLocation = this.OriginalLocation,
                FileSizeMb = this.FileSizeMb,
                FileSizeAbove = this.FileSizeAbove,
            };

            await new RecycleBinReport(appInfo, uiLog, parameters).RunAsync();
        }
    }
}
