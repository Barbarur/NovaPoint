using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
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
    /// Interaction logic for CopyDuplicateFileAutoForm.xaml
    /// </summary>
    public partial class CopyDuplicateFileAutoForm : Page, ISolutionForm
    {
        public string SourceSiteURL { get; set; } = string.Empty;
        public string SourceListTitle { get; set; } = string.Empty;

        public bool IsMove { get; set; } = true;

        public string DestinationSiteURL { get; set; } = string.Empty;
        public string DestinationListTitle { get; set; } = string.Empty;
        public string DestinationFolderServerRelativeUrl { get; set; } = string.Empty;

        public CopyDuplicateFileAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = CopyDuplicateFileAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(CopyDuplicateFileAuto);
            SolutionHeader.SolutionDocs = CopyDuplicateFileAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            CopyDuplicateFileAutoParameters parameters = new(
                ModeF.ReportMode,
                IsMove,
                AdminF.Parameters,
                SourceSiteURL,
                SourceListTitle,
                ItemF.Parameters,
                DestinationSiteURL,
                DestinationListTitle,
                DestinationFolderServerRelativeUrl);

            await CopyDuplicateFileAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }

    }
}
