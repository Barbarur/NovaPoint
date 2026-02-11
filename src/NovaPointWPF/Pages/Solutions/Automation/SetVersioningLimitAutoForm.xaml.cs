using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class SetVersioningLimitAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;


        public SetVersioningLimitAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = SetVersioningLimitAuto.s_SolutionName;
            SolutionCode = nameof(SetVersioningLimitAuto);
            SolutionDocs = SetVersioningLimitAuto.s_SolutionDocs;

            SolutionCreate = SetVersioningLimitAuto.Create;

            this.LibraryMajorVersionLimit = 500;
            this.LibraryMinorVersionLimit = 0;
            this.ListMajorVersionLimit = 500;
        }

        public ISolutionParameters GetParameters()
        {
            SetVersioningLimitAutoParameters parameters = new(AdminF.Parameters, SiteF.Parameters, VersioningF.Parameters);
            return parameters;
        }
    }
}
