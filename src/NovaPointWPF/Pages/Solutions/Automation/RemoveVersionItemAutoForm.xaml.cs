using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public partial class RemoveVersionItemAutoForm : Page, ISolutionForm
    {
        public string SiteUrl { get; set; }

        private bool _listAll;
        public bool ListAll
        {
            get { return _listAll; }
            set
            {
                _listAll = value;
                if (value)
                {
                    LibrarynameLabel.Visibility = Visibility.Collapsed;
                }
                else if (!value)
                {
                    LibrarynameLabel.Visibility = Visibility.Visible;
                }
            }
        }
        public string ListName { get; set; }

        private bool _itemAll;
        public bool ItemsAll
        {
            get { return _itemAll; }
            set
            {
                _itemAll = value;
                if (value)
                {
                    RelativeUrlLabel.Visibility = Visibility.Collapsed;
                }
                else if (!value)
                {
                    RelativeUrlLabel.Visibility = Visibility.Visible;
                }
            }
        }
        public string RelativePath { get; set; }

        private bool _deleteAll;
        public bool DeleteAll
        {
            get { return _deleteAll; }
            set
            {
                _deleteAll = value;
                if (value)
                {
                    VersionsKeepLabel.Visibility = Visibility.Collapsed;
                    RecycleCheckBox.Visibility = Visibility.Collapsed;
                }
                else if (!value)
                {
                    VersionsKeepLabel.Visibility = Visibility.Visible;
                    RecycleCheckBox.Visibility = Visibility.Visible;
                }
            }
        }

        public int VersionsToKeep { get; set; }
        public bool Recycle { get; set; }

        public bool ReportMode { get; set; }

        public RemoveVersionItemAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SiteUrl = String.Empty;

            ListAll = true;
            ListName = String.Empty;

            ItemsAll = true;
            RelativePath = String.Empty;

            DeleteAll = true;
            VersionsToKeep = 100;
            Recycle = true;

            ReportMode = true;


            SolutionHeader.SolutionTitle = RemoveFileVersionAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveFileVersionAuto);
            SolutionHeader.SolutionDocs = RemoveFileVersionAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            RemoveFileVersionAutoParameters parameters = new()
            {
                SiteUrl = SiteUrl,

                ListAll = ListAll,
                ListTitle = ListName,

                ItemsAll = ItemsAll,
                FolderRelativeUrl = RelativePath,

                DeleteAll = DeleteAll,
                VersionsToKeep = VersionsToKeep,
                Recycle = Recycle,

                ReportMode = ReportMode
            };
            //await new RemoveFileVersionAuto(uiLog, appInfo, parameters).RunAsync();
        }
    }
}
