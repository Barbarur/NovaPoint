using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class SetSiteCollectionAdminAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

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

            SolutionName = SetSiteCollectionAdminAuto.s_SolutionName;
            SolutionCode = nameof(SetSiteCollectionAdminAuto);
            SolutionDocs = SetSiteCollectionAdminAuto.s_SolutionDocs;

            SolutionCreate = SetSiteCollectionAdminAuto.Create;

            this.TargetUserUPN = string.Empty;
            this.IsSiteAdmin = false;
        }

        public ISolutionParameters GetParameters()
        {
            var siteParam = SiteF.Parameters;

            SetSiteCollectionAdminAutoParameters parameters = new(siteParam)
            {
                TargetUserUPN = this.TargetUserUPN,
                IsSiteAdmin = this.IsSiteAdmin,
            };

            return parameters;
        }

    }
}
