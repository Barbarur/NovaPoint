using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.QuickFix;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.QuickFix
{
    public partial class IdMismatchTroubleForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public string UserUpn { get; set; }

        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = IdMismatchTrouble.s_SolutionName;
            SolutionCode = nameof(IdMismatchTrouble);
            SolutionDocs = IdMismatchTrouble.s_SolutionDocs;

            SolutionCreate = IdMismatchTrouble.Create;

            this.UserUpn = string.Empty;
        }

        public ISolutionParameters GetParameters()
        {
            IdMismatchTroubleParameters parameters = new(AdminF.Parameters, SiteF.Parameters)
            {
                ReportMode = Mode.ReportMode,
                UserUpn = this.UserUpn,
            };
            return parameters;
        }
    }
}
