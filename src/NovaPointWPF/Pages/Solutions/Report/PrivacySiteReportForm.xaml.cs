using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class PrivacySiteReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public PrivacySiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = PrivacySiteReport.s_SolutionName;
            SolutionCode = nameof(PrivacySiteReport);
            SolutionDocs = PrivacySiteReport.s_SolutionDocs;

            SolutionCreate = PrivacySiteReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            PrivacySiteReportParameters parameters = new();
            return parameters;
        }

    }
}
