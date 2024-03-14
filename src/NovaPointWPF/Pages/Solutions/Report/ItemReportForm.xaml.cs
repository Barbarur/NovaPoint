using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using PnP.Framework.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ItemReportForm.xaml
    /// </summary>
    public partial class ItemReportForm : Page, ISolutionForm
    {

        public ItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ItemReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ItemReport);
            SolutionHeader.SolutionDocs = ItemReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            var listParameters = ListForm.Parameters;

            SPOTenantListsParameters tListParam = new(siteAccParam, listParameters);

            var itemParameters = ItemForm.Parameters;

            ItemReportParameters parameters = new(tListParam, itemParameters);

            //await new ItemReport(parameters, uiLog, cancelTokenSource).RunAsync();

            await ItemReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
