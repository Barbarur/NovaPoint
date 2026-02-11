using Microsoft.Win32;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class RemoveSiteAutoForm : Page, ISolutionForm
    {
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
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
                ListOfSitesPath = openFileDialog.FileName;
        }

        public ISolutionParameters GetParameters()
        {
            RemoveSiteAutoParameters parameters = new(ListOfSitesPath);
            return parameters;
        }
    }
}
