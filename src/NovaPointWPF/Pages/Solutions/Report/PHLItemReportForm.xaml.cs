using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class PHLItemReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public PHLItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = PHLItemReport.s_SolutionName;
            SolutionCode = nameof(PHLItemReport);
            SolutionDocs = PHLItemReport.s_SolutionDocs;

            SolutionCreate = PHLItemReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            PHLItemReportParameters parameters = new(PHLForm.Parameters, AdminF.Parameters, SiteF.Parameters, new(), new());
            return parameters;
        }

    }
}
