using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class CopyDuplicateFileAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public string SourceSiteURL { get; set; } = string.Empty;
        public string SourceListTitle { get; set; } = string.Empty;

        public bool IsMove { get; set; } = true;

        public string DestinationSiteURL { get; set; } = string.Empty;
        public string DestinationListTitle { get; set; } = string.Empty;
        public string DestinationFolderServerRelativeUrl { get; set; } = string.Empty;

        public CopyDuplicateFileAutoForm()
        {
            InitializeComponent();

            SolutionName = CopyDuplicateFileAuto.s_SolutionName;
            SolutionCode = nameof(CopyDuplicateFileAuto);
            SolutionDocs = CopyDuplicateFileAuto.s_SolutionDocs;

            SolutionCreate = CopyDuplicateFileAuto.Create;

            DataContext = this;
        }

        public ISolutionParameters GetParameters()
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

            return parameters;
        }

    }
}
