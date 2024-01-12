using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveSiteUserAutoForm.xaml
    /// </summary>
    public partial class RemoveSiteUserAutoForm : Page, ISolutionForm
    {
        public string DeleteUserUPN { get; set; }

        public string AdminUPN { get; set; }
        public bool RemoveAdmin { get; set; }

        public bool SiteAll { get; set; }
        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public RemoveSiteUserAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveSiteUserAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveSiteUserAuto);
            SolutionHeader.SolutionDocs = RemoveSiteUserAuto.s_SolutionDocs;

            DeleteUserUPN = string.Empty;

            this.AdminUPN = String.Empty;
            this.RemoveAdmin = true;

            this.SiteAll = true;
            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            RemoveUserAutoParameters parameters = new()
            {
                DeleteUserUPN = this.DeleteUserUPN,

                AdminUPN = this.AdminUPN,
                RemoveAdmin = this.RemoveAdmin,

                SiteAll = this.SiteAll,
                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,
            };
            await new RemoveSiteUserAuto(parameters, uiLog, cancelTokenSource).RunAsync();

        }
    }
}
