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


        public bool ListAll
        {
            get { return (bool)GetValue(ListAllProperty); }
            set { SetValue(ListAllProperty, value); }
        }
        public static readonly DependencyProperty ListAllProperty =
            DependencyProperty.Register("ListAll", typeof(bool), typeof(ListForm), new FrameworkPropertyMetadata(defaultValue: false));


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



        private bool _singleList;
        public bool SingleList
        {
            get { return _singleList; }
            set
            {
                _singleList = value;
                if (value)
                {
                    AllListsFilter.Visibility = Visibility.Collapsed;

                    SingleListTitle.Visibility = Visibility.Visible;
                }
                else
                {
                    AllListsFilter.Visibility = Visibility.Visible;

                    SingleListTitle.Visibility = Visibility.Collapsed;
                    ListTitle = string.Empty;
                }
            }
        }


        public string ListTitle
        {
            get { return (string)GetValue(ListTitleProperty); }
            set { SetValue(ListTitleProperty, value); }
        }
        public static readonly DependencyProperty ListTitleProperty =
            DependencyProperty.Register("ListTitle", typeof(string), typeof(ListForm), new PropertyMetadata(string.Empty));



    }
}
