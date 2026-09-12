using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.Platform;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointWPF.Platform;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class RemoveSiteAutoForm : Page, ISolutionForm
    {
        private readonly IFilePicker _filePicker = new WpfFilePicker();

        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        private string _listOfSitesPath = string.Empty;
        public string ListOfSitesPath
        {
            get { return _listOfSitesPath; }
            set
            {
                _listOfSitesPath = value;
                PathLabel.Text = value;
            }
        }

        public RemoveSiteAutoForm()
        {
            InitializeComponent();

            SolutionName = RemoveSiteAuto.s_SolutionName;
            SolutionCode = nameof(RemoveSiteAuto);
            SolutionDocs = RemoveSiteAuto.s_SolutionDocs;

            SolutionCreate = RemoveSiteAuto.Create;

            DataContext = this;
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            // WpfFilePicker's Task completes synchronously (the dialog itself blocks), so this
            // never actually waits on anything - safe without an async handler.
            string? path = _filePicker.PickFileAsync(title: "", filter: "").GetAwaiter().GetResult();
            if (path is not null)
                ListOfSitesPath = path;
        }

        public ISolutionParameters GetParameters()
        {
            RemoveSiteAutoParameters parameters = new(ListOfSitesPath);
            return parameters;
        }
    }
}
