using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class RecycleBinReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public RecycleBinReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = RecycleBinReport.s_SolutionName;
            SolutionCode = nameof(RecycleBinReport);
            SolutionDocs = RecycleBinReport.s_SolutionDocs;

            SolutionCreate = RecycleBinReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            RecycleBinReportParameters parameters = new(RecycleF.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }

    }
}
