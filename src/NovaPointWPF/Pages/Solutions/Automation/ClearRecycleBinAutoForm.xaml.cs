using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for ClearRecycleBinAutoForm.xaml
    /// </summary>
    public partial class ClearRecycleBinAutoForm : Page, ISolutionForm
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

        public ClearRecycleBinAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ClearRecycleBinAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ClearRecycleBinAuto);
            SolutionHeader.SolutionDocs = ClearRecycleBinAuto.s_SolutionDocs;

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
            ClearRecycleBinAutoParameters parameters = new()
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

            await new ClearRecycleBinAuto(appInfo, uiLog, parameters).RunAsync();

        }
    }
}
