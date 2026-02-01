using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Directory;
using NovaPointWPF.Pages.SolutionsFormParameters;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Directory
{
    /// <summary>
    /// Interaction logic for GetDirectoryGroupForm.xaml
    /// </summary>
    public partial class GetDirectoryGroupForm : Page, ISolutionForm
    {
        public GetDirectoryGroupForm()
        {
            InitializeComponent();
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            GetDirectoryGroupParameters parameters = new GetDirectoryGroupParameters(GroupF.Parameters);

            await GetDirectoryGroup.RunAsync(parameters, uiLog, cancelTokenSource);
        }

    }
}
