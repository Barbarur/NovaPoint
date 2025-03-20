using Microsoft.Win32;
using NovaPointLibrary.Commands.SharePoint.List;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.UserControls
{

    public partial class ListForm : UserControl, INotifyPropertyChanged
    {
        public SPOListsParameters Parameters { get; set; } = new();

        private string _filterTarget = "Both";
        public string FilterTarget
        {
            get { return _filterTarget; }
            set
            {
                _filterTarget = value;
                if (value == "List")
                {
                    FilterTitleLabel.Title = "List filter";
                    AllButton.Content = "All lists";
                }
                else if (value == "Library")
                {
                    FilterTitleLabel.Title = "Library filter";
                    AllButton.Content = "All libraries";
                }
                else
                {
                    FilterTitleLabel.Title = "Library and List filter";
                    AllButton.Content = "All libraries and lists";
                }
            }
        }

        public bool AllLists
        {
            get { return Parameters.AllLists; }
            set
            {
                Parameters.AllLists = value;
                if (value)
                {
                    if (ListsFilterVisibility)
                    {
                        AllFilterStack.Visibility = Visibility.Visible;
                        IncludeLists = true;
                        IncludeLibraries = true;
                    }
                }
                else
                {
                    AllFilterStack.Visibility = Visibility.Collapsed;
                    IncludeLists = false;
                    IncludeLibraries = false;
                    IncludeHiddenLists = false;
                    IncludeSystemLists = false;
                }
                OnPropertyChanged();
            }
        }


        private bool _listsFilterVisibility = true;
        public bool ListsFilterVisibility
        {
            get { return _listsFilterVisibility; }
            set
            {
                _listsFilterVisibility = value;
                if (value)
                {
                    AllFilterStack.Visibility = Visibility.Visible;
                }
                else
                {
                    AllFilterStack.Visibility = Visibility.Collapsed;
                }
            }
        }


        public bool IncludeLibraries
        {
            get { return Parameters.IncludeLibraries; }
            set
            {
                Parameters.IncludeLibraries = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeLists
        {
            get { return Parameters.IncludeLists; }
            set
            {
                Parameters.IncludeLists = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeHiddenLists
        {
            get { return Parameters.IncludeHiddenLists; }
            set
            {
                Parameters.IncludeHiddenLists = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeSystemLists
        {
            get { return Parameters.IncludeSystemLists; }
            set
            {
                Parameters.IncludeSystemLists = value;
                OnPropertyChanged();
            }
        }


        private bool _collectionLists = false;
        public bool CollectionLists
        {
            get { return _collectionLists; }
            set
            {
                _collectionLists = value;
                if (value) { CollectionListsPanel.Visibility = Visibility.Visible; }
                else
                {
                    CollectionListsPanel.Visibility = Visibility.Collapsed;
                    CollectionListsPath = string.Empty;
                }
                OnPropertyChanged();
            }
        }

        public string CollectionListsPath
        {
            get { return Parameters.CollectionListsPath; }
            set
            {
                Parameters.CollectionListsPath = value;
                OnPropertyChanged();
            }
        }


        private bool _singleList = false;
        public bool SingleList
        {
            get { return _singleList; }
            set
            {
                _singleList = value;
                if (value) { SingleListTitle.Visibility = Visibility.Visible; }
                else
                {
                    ListTitle = string.Empty;
                    SingleListTitle.Visibility = Visibility.Collapsed;
                }
                OnPropertyChanged();
            }
        }

        public string ListTitle
        {
            get { return Parameters.ListTitle; }
            set
            {
                Parameters.ListTitle = value;
                OnPropertyChanged();
            }
        }


        public ListForm()
        {
            InitializeComponent();
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                CollectionListsPath = openFileDialog.FileName;
        }

    }
}
