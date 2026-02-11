using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ItemReportForm.xaml
    /// </summary>
    public partial class ItemReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public ItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = ItemReport.s_SolutionName;
            SolutionCode = nameof(ItemReport);
            SolutionDocs = ItemReport.s_SolutionDocs;

            SolutionCreate = ItemReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            ItemReportParameters parameters = new(AdminF.Parameters, SiteF.Parameters, ListForm.Parameters, ItemForm.Parameters);
            return parameters;
        }
    }
}
