using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class PageAssetsReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public PageAssetsReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = PageAssetsReport.s_SolutionName;
            SolutionCode = nameof(PageAssetsReport);
            SolutionDocs = PageAssetsReport.s_SolutionDocs;

            SolutionCreate = PageAssetsReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            PageAssetsReportParameters parameters = new(AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }

    }
}
