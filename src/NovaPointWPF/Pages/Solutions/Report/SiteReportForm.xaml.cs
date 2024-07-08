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
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointWPF.UserControls;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Item;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SiteReportForm.xaml
    /// </summary>
    public partial class SiteReportForm : Page, ISolutionForm
    {
        public bool Detailed { get; set; }

        public SiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SiteReport);
            SolutionHeader.SolutionDocs = SiteReport.s_SolutionDocs;

            this.Detailed = true;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            siteAccParam.SiteParam = SiteF.Parameters;

            SiteReportParameters parameters = new(siteAccParam, this.Detailed);

            await SiteReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
