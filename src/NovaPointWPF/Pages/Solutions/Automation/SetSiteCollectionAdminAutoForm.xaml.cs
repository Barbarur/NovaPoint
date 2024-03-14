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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for SetSiteCollectionAdminAutoForm.xaml
    /// </summary>
    public partial class SetSiteCollectionAdminAutoForm : Page, ISolutionForm
    {
        public string TargetUserUPN { get; set; } = string.Empty;

        private bool _isSiteAdmin = false;
        public bool IsSiteAdmin
        {
            get {  return _isSiteAdmin; }
            set { _isSiteAdmin = value; }
        }

        public SetSiteCollectionAdminAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SetSiteCollectionAdminAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SetSiteCollectionAdminAuto);
            SolutionHeader.SolutionDocs = SetSiteCollectionAdminAuto.s_SolutionDocs;

            this.TargetUserUPN = string.Empty;
            this.IsSiteAdmin = false;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteParam = SiteF.Parameters;

            SetSiteCollectionAdminAutoParameters parameters = new(siteParam)
            {
                TargetUserUPN = this.TargetUserUPN,
                IsSiteAdmin = this.IsSiteAdmin,
            };

            //await new SetSiteCollectionAdminAuto(parameters, uiLog, cancelTokenSource).RunAsync();

            await SetSiteCollectionAdminAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
