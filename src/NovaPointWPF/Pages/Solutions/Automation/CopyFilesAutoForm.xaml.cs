using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for CopyFilesAutoForm.xaml
    /// </summary>
    public partial class CopyFilesAutoForm : Page, ISolutionForm
    {
        public string SourceSiteURL { get; set; } = string.Empty;
        public string SourceListTitle { get; set; } = string.Empty;


        public string DestinationSiteURL { get; set; } = string.Empty;
        public string DestinationListTitle { get; set; } = string.Empty;
        public string DestinationFolderServerRelativeUrl { get; set; } = string.Empty;

        public CopyFilesAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = CopyFilesAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(CopyFilesAuto);
            SolutionHeader.SolutionDocs = CopyFilesAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            CopyFilesAutoParameters parameters = new(ModeF.ReportMode, AdminF.Parameters,
                                                     SourceSiteURL, SourceListTitle, ItemF.Parameters,
                                                     DestinationSiteURL, DestinationListTitle, DestinationFolderServerRelativeUrl);

            await CopyFilesAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
