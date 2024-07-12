using NovaPointLibrary.Commands.SharePoint.List;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for ListForm.xaml
    /// </summary>
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
                    MainLabel.Text= "List filter";
                    AllButton.Content = "All lists";
                    SingleButton.Content = "Single list";
                }
                else if (value == "Library")
                {
                    MainLabel.Text = "Library filter";
                    AllButton.Content = "All libraries";
                    SingleButton.Content = "Single library";
                }
                else
                {
                    MainLabel.Text = "Library and List filter";
                    AllButton.Content = "All libraries and lists";
                    SingleButton.Content = "Single library or list";
                }
            }
        }

        private bool _allLists = true;
        public bool AllLists
        {
            get { return _allLists; }
            set
            {
                _allLists = value;
                Parameters.AllLists = value;
                if (value)
                {
                    if (ListsFilterVisibility) { AllFilterStack.Visibility = Visibility.Visible; }
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


        public bool _includeLibraries = true;
        public bool IncludeLibraries
        {
            get { return _includeLibraries; }
            set
            {
                _includeLibraries = value;
                Parameters.IncludeLibraries = value;
                OnPropertyChanged();
            }
        }

        private bool _includeLists = true;
        public bool IncludeLists
        {
            get { return _includeLists; }
            set
            {
                _includeLists = value;
                Parameters.IncludeLists = value;
                OnPropertyChanged();
            }
        }

        private bool _includeHiddenLists = false;
        public bool IncludeHiddenLists
        {
            get { return _includeHiddenLists; }
            set
            {
                _includeHiddenLists = value;
                Parameters.IncludeHiddenLists = value;
                OnPropertyChanged();
            }
        }

        private bool _includeSystemLists = false;
        public bool IncludeSystemLists
        {
            get { return _includeSystemLists; }
            set
            {
                _includeSystemLists = value;
                Parameters.IncludeSystemLists = value;
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

        private string _listTitle = string.Empty;
        public string ListTitle
        {
            get { return _listTitle; }
            set
            {
                _listTitle = value;
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
    }
}
