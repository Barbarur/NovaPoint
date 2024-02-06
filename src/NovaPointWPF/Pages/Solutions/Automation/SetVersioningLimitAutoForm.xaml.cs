using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for SetVersioningLimitAutoForm.xaml
    /// </summary>
    public partial class SetVersioningLimitAutoForm : Page, ISolutionForm
    {
        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public bool IncludeLists { get; set; }
        public bool IncludeLibraries { get; set; }
        public bool IncludeHiddenLists { get; set; }
        public bool IncludeSystemLists { get; set; }
        public string ListTitle { get; set; }

        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;


        public SetVersioningLimitAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SetVersioningLimitAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SetVersioningLimitAuto);
            SolutionHeader.SolutionDocs = SetVersioningLimitAuto.s_SolutionDocs;

            this.RemoveAdmin = true;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;

            this.IncludeLists = true;
            this.IncludeLibraries = true;
            this.IncludeHiddenLists = false;
            this.IncludeSystemLists = false;
            this.ListTitle = String.Empty;

            this.LibraryMajorVersionLimit = 500;
            this.LibraryMinorVersionLimit = 0;
            this.ListMajorVersionLimit = 500;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SetVersioningLimitAutoParameters parameters = new()
            {
                RemoveAdmin = this.RemoveAdmin,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,

                IncludeLists = this.IncludeLists,
                IncludeLibraries = this.IncludeLibraries,
                IncludeHiddenLists = this.IncludeHiddenLists,
                IncludeSystemLists = this.IncludeSystemLists,
                ListTitle = this.ListTitle,

                LibraryMajorVersionLimit = this.LibraryMajorVersionLimit,
                LibraryMinorVersionLimit = this.LibraryMinorVersionLimit,
                ListMajorVersionLimit = this.ListMajorVersionLimit,
            };

            await new SetVersioningLimitAuto(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
