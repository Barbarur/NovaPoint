using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Directory;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveFileVersionAutoForm.xaml
    /// </summary>
    public partial class RemoveFileVersionAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public RemoveFileVersionAutoForm()
        {
            InitializeComponent();

            SolutionName = RemoveFileVersionAuto.s_SolutionName;
            SolutionCode = nameof(RemoveFileVersionAuto);
            SolutionDocs = RemoveFileVersionAuto.s_SolutionDocs;

            SolutionCreate = RemoveFileVersionAuto.Create;

            DataContext = this;
        }

        public ISolutionParameters GetParameters()
        {
            RemoveFileVersionAutoParameters parameters = new(Mode.ReportMode, VersionF.Parameters, AdminF.Parameters,
                SiteF.Parameters, ListForm.Parameters, ItemForm.Parameters);
            return parameters;
        }
    }
}
