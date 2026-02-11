using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class ListReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public bool IncludeStorageMetrics { get; set; } = true;

        public ListReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = ListReport.s_SolutionName;
            SolutionCode = nameof(ListReport);
            SolutionDocs = ListReport.s_SolutionDocs;

            SolutionCreate = ListReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            ListReportParameters parameters = new(IncludeStorageMetrics, AdminF.Parameters, SiteF.Parameters, ListForm.Parameters);
            return parameters;
        }
    }
}
