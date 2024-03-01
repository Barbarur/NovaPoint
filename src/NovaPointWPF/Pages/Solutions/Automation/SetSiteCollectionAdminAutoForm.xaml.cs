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
        public string TargetUserUPN { get; set; }
        public bool IsSiteAdmin { get; set; }


        //public bool IncludePersonalSite { get; set; }
        //public bool IncludeShareSite { get; set; }
        //public bool OnlyGroupIdDefined { get; set; }
        //public string SiteUrl { get; set; }
        //public bool IncludeSubsites { get; set; }

        public SetSiteCollectionAdminAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SetSiteCollectionAdminAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SetSiteCollectionAdminAuto);
            SolutionHeader.SolutionDocs = SetSiteCollectionAdminAuto.s_SolutionDocs;

            this.TargetUserUPN = string.Empty;
            this.IsSiteAdmin = true;

            //this.IncludePersonalSite = false;
            //this.IncludeShareSite = true;
            //this.OnlyGroupIdDefined = false;
            //this.SiteUrl = String.Empty;
            //this.IncludeSubsites = false;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            //SetSiteCollectionAdminAutoParameters parameters = new()
            //{
            //    TargetUserUPN = this.TargetUserUPN,
            //    IsSiteAdmin = this.IsSiteAdmin,

            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,
            //    IncludeSubsites = this.IncludeSubsites,
            //};

            //await new SetSiteCollectionAdminAuto(parameters, uiLog, cancelTokenSource).RunAsync();


            var siteParam = SiteF.Parameters;

            SetSiteCollectionAdminAutoParameters parameters = new(siteParam)
            {
                TargetUserUPN = this.TargetUserUPN,
                IsSiteAdmin = this.IsSiteAdmin,
            };

            await new SetSiteCollectionAdminAuto(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
