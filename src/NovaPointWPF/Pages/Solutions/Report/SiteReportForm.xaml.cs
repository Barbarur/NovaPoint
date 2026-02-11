using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class SiteReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public SiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = SiteReport.s_SolutionName;
            SolutionCode = nameof(SiteReport);
            SolutionDocs = SiteReport.s_SolutionDocs;

            SolutionCreate = SiteReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            SiteReportParameters parameters = new(SiteDetails.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }
    }
}
