using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Commands.SharePoint.Site;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemovePHLItemAutoForm.xaml
    /// </summary>
    public partial class RemovePHLItemAutoForm : Page, ISolutionForm
    {
        public bool Recycle { get; set; } = true;

        public RemovePHLItemAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemovePHLItemAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemovePHLItemAuto);
            SolutionHeader.SolutionDocs = RemovePHLItemAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            RemovePHLItemAutoParameters parameters = new(PHLForm.Parameters, AdminF.Parameters,
                SiteF.Parameters, new(), new())
            {
                Recycle = this.Recycle,
            };

            await RemovePHLItemAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
