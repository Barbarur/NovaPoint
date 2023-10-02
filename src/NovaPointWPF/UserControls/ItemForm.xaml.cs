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
    /// Interaction logic for ItemForm.xaml
    /// </summary>
    public partial class ItemForm : UserControl
    {
        public ItemForm()
        {
            InitializeComponent();

        }

        public bool ItemsAll
        {
            get { return (bool)GetValue(ItemsAllProperty); }
            set { SetValue(ItemsAllProperty, value); }
        }
        public static readonly DependencyProperty ItemsAllProperty =
            DependencyProperty.Register("ItemsAll", typeof(bool), typeof(ItemForm), new FrameworkPropertyMetadata(defaultValue: false));

        private bool _relativeUrl;
        public bool RelativeUrl
        {
            get { return _relativeUrl; }
            set
            {
                _relativeUrl = value;
                if (value)
                {
                    SpecificRelativeUrl.Visibility = Visibility.Visible;
                }
                else
                {
                    SpecificRelativeUrl.Visibility = Visibility.Collapsed;
                    FolderRelativeUrl = string.Empty;
                }
            }
        }


        public string FolderRelativeUrl
        {
            get { return (string)GetValue(FolderRelativeUrlProperty); }
            set { SetValue(FolderRelativeUrlProperty, value); }
        }
        public static readonly DependencyProperty FolderRelativeUrlProperty =
            DependencyProperty.Register("FolderRelativeUrl", typeof(string), typeof(ItemForm), new PropertyMetadata(string.Empty));

    }
}
