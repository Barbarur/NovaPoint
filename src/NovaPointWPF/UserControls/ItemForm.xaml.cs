using NovaPointLibrary.Commands.SharePoint.Item;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for ItemForm.xaml
    /// </summary>
    public partial class ItemForm : UserControl, INotifyPropertyChanged
    {
        
        private string _filterTarget = "Both";
        public string FilterTarget
        {
            get { return _filterTarget; }
            set
            {
                _filterTarget = value;
                if (value == "List")
                {
                    MainLabel.Text = "Item filter";
                    AllButton.Content = "All Items";
                }
                else if (value == "Library")
                {
                    MainLabel.Text = "File filter";
                    AllButton.Content = "All files";
                }
                else
                {
                    MainLabel.Text = "Files and Items filter";
                    AllButton.Content = "All files and items";
                }
            }
        }

        public SPOItemsParameters Parameters { get; set; } = new();


        private bool _itemsAll = true;
        public bool ItemsAll
        {
            get { return _itemsAll; }
            set
            {
                _itemsAll = value;
                Parameters.AllItems = value;
                OnPropertyChanged();
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
                    FolderSiteRelativeUrl = string.Empty;
                }
            }
        }

        private string _folderSiteRelativeUrl = string.Empty;
        public string FolderSiteRelativeUrl
        {
            get { return _folderSiteRelativeUrl; }
            set
            {
                _folderSiteRelativeUrl = value;
                Parameters.FolderSiteRelativeUrl = value;
                OnPropertyChanged();
            }
        }


        public ItemForm()
        {
            InitializeComponent();

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
