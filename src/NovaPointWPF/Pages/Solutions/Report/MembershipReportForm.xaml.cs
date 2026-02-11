using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class MembershipReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public MembershipReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = MembershipReport.s_SolutionName;
            SolutionCode = nameof(MembershipReport);
            SolutionDocs = MembershipReport.s_SolutionDocs;

            SolutionCreate = MembershipReport.Create;
        }

        public ISolutionParameters GetParameters()
        {
            MembershipReportParameters parameters = new(MembershipF.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }
    }
}
