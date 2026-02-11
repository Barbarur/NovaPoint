using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class ShortcutODReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public ShortcutODReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = ShortcutODReport.s_SolutionName;
            SolutionCode = nameof(ShortcutODReport);
            SolutionDocs = ShortcutODReport.s_SolutionDocs;

            SolutionCreate = ShortcutODReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            ShortcutODReportParameters parameters = new(AdminF.Parameters, SiteF.Parameters, ItemForm.Parameters);
            return parameters;
        }

    }
}
