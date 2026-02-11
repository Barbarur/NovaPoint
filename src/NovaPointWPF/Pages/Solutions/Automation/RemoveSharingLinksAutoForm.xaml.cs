using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Directory;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveSharingLinksAutoForm.xaml
    /// </summary>
    public partial class RemoveSharingLinksAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public RemoveSharingLinksAutoForm()
        {
            InitializeComponent();

            SolutionName = RemoveSharingLinksAuto.s_SolutionName;
            SolutionCode = nameof(RemoveSharingLinksAuto);
            SolutionDocs = RemoveSharingLinksAuto.s_SolutionDocs;

            SolutionCreate = RemoveSharingLinksAuto.Create;

            DataContext = this;
        }

        public ISolutionParameters GetParameters()
        {
            RemoveSharingLinksAutoParameters parameters = new(LinkF.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }
    }
}
