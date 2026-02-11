using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Directory;
using NovaPointLibrary.Solutions.Report;
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
    /// Interaction logic for RemovePHLItemAutoForm.xaml
    /// </summary>
    public partial class RemovePHLItemAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }


        public bool Recycle { get; set; } = true;

        public RemovePHLItemAutoForm()
        {
            InitializeComponent();

            SolutionName = RemovePHLItemAuto.s_SolutionName;
            SolutionCode = nameof(RemovePHLItemAuto);
            SolutionDocs = RemovePHLItemAuto.s_SolutionDocs;

            SolutionCreate = RemovePHLItemAuto.Create;

            DataContext = this;
        }

        public ISolutionParameters GetParameters()
        {
            RemovePHLItemAutoParameters parameters = new(PHLForm.Parameters, AdminF.Parameters,
                SiteF.Parameters, new(), new())
            {
                Recycle = this.Recycle,
            };
            return parameters;
        }
    }
}
