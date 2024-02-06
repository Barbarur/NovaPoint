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

namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for ListForm.xaml
    /// </summary>
    public partial class ListForm : UserControl
    {
        public ListForm()
        {
            InitializeComponent();
        }

        private string _filterTarget = "Both";
        public string FilterTarget
        {
            get { return _filterTarget; }
            set
            {
                _filterTarget = value;
                if (value == "List")
                {
                    MainLabel.Content = "List filter";
                    AllButton.Content = "All lists";
                    SingleButton.Content = "Single list";
                }
                else if(value == "Library")
                {
                    MainLabel.Content = "Library filter";
                    AllButton.Content = "All libraries";
                    SingleButton.Content = "Single library";
                }
                else
                {
                    MainLabel.Content = "Library and List filter";
                    AllButton.Content = "All libraries and lists";
                    SingleButton.Content = "Single library or list";
                }
            }
        }

        private bool _singleList = false;
        public bool SingleList
        {
            get { return _singleList; }
            set
            {
                _singleList = value;
                if (value)
                {
                    AllFilterStack.Visibility = Visibility.Collapsed;

                    SingleListTitle.Visibility = Visibility.Visible;
                }
                else
                {
                    if (ListsFilterVisibility) { AllFilterStack.Visibility = Visibility.Visible; }

                    SingleListTitle.Visibility = Visibility.Collapsed;
                    ListTitle = string.Empty;
                }
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
            get { return (bool)GetValue(IncludeLibrariesProperty); }
            set { SetValue(IncludeLibrariesProperty, value); }
        }
        public static readonly DependencyProperty IncludeLibrariesProperty =
            DependencyProperty.Register("IncludeLibraries", typeof(bool), typeof(ListForm), new FrameworkPropertyMetadata(defaultValue: true));

        public bool IncludeLists
        {
            get { return (bool)GetValue(IncludeListsProperty); }
            set { SetValue(IncludeListsProperty, value); }
        }
        public static readonly DependencyProperty IncludeListsProperty =
            DependencyProperty.Register("IncludeLists", typeof(bool), typeof(ListForm), new FrameworkPropertyMetadata(defaultValue: true));

        public bool IncludeHiddenLists
        {
            get { return (bool)GetValue(IncludeHiddenListsProperty); }
            set { SetValue(IncludeHiddenListsProperty, value); }
        }
        public static readonly DependencyProperty IncludeHiddenListsProperty =
            DependencyProperty.Register("IncludeHiddenLists", typeof(bool), typeof(ListForm), new FrameworkPropertyMetadata(defaultValue: false));

        public bool IncludeSystemLists
        {
            get { return (bool)GetValue(IncludeSystemListsProperty); }
            set { SetValue(IncludeSystemListsProperty, value); }
        }
        public static readonly DependencyProperty IncludeSystemListsProperty =
            DependencyProperty.Register("IncludeSystemLists", typeof(bool), typeof(ListForm), new FrameworkPropertyMetadata(defaultValue: false));

        
        public string ListTitle
        {
            get { return (string)GetValue(ListTitleProperty); }
            set { SetValue(ListTitleProperty, value); }
        }
        public static readonly DependencyProperty ListTitleProperty =
            DependencyProperty.Register("ListTitle", typeof(string), typeof(ListForm), new PropertyMetadata(string.Empty));

    }
}
