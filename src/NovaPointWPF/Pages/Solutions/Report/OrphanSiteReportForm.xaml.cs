using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{

    public partial class OrphanSiteReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public OrphanSiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = OrphanSiteReport.s_SolutionName;
            SolutionCode = nameof(OrphanSiteReport);
            SolutionDocs = OrphanSiteReport.s_SolutionDocs;

            SolutionCreate = OrphanSiteReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            OrphanSiteReportParameters parameters = new(SiteF.Parameters);
            return parameters;
        }

    }
}
