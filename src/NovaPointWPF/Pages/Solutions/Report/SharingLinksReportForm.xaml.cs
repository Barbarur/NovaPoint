using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class SharingLinksReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public bool BreakdownInvitations { get; set; } = true;

        public SharingLinksReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = SharingLinksReport.s_SolutionName;
            SolutionCode = nameof(SharingLinksReport);
            SolutionDocs = SharingLinksReport.s_SolutionDocs;

            SolutionCreate = SharingLinksReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            SharingLinksReportParameters parameters = new(BreakdownInvitations, LinkF.Parameters, SiteF.Parameters, AdminF.Parameters);
            return parameters;
        }

    }
}
