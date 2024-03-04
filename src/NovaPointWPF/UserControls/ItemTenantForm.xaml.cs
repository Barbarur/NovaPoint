using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.User;
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
    /// Interaction logic for ItemTenantForm.xaml
    /// </summary>
    public partial class ItemTenantForm : UserControl, INotifyPropertyChanged
    {
        public SPOItemsParameters Parameters { get; set; } = new();

        private string _filterTarget = "Both";
        public string FilterTarget
        {
            get { return _filterTarget; }
            set
            {
                _filterTarget = value;
                if (value == "List")
                {
                    MainLabel.Content = "Item filter";
                    AllButton.Content = "All Items";
                }
                else if (value == "Library")
                {
                    MainLabel.Content = "File filter";
                    AllButton.Content = "All files";
                }
                else
                {
                    MainLabel.Content = "Files and Items filter";
                    AllButton.Content = "All files and items";
                }
            }
        }


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


        private string _folderRelativeUrl = string.Empty;
        public string FolderRelativeUrl
        {
            get { return _folderRelativeUrl; }
            set
            {
                _folderRelativeUrl = value;
                Parameters.FolderRelativeUrl = value;
                OnPropertyChanged();
            }
        }

        public ItemTenantForm()
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
