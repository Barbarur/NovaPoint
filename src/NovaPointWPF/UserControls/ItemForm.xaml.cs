using NovaPointLibrary.Commands.SharePoint.Item;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.UserControls
{
    public partial class ItemForm : UserControl, INotifyPropertyChanged
    {
        public SPOItemsParameters Parameters { get; set; } = new();

        public bool ItemsAll
        {
            get { return Parameters.AllItems; }
            set
            {
                Parameters.AllItems = value;
                OnPropertyChanged();
            }
        }

        private bool _filterItems;
        public bool FilterItems
        {
            get { return _filterItems; }
            set
            {
                _filterItems = value;
                if (value)
                {
                    FilterPanel.Visibility = Visibility.Visible;
                    ComboBoxCreatedAfter.Reset();
                    ComboBoxCreatedBefore.Reset();
                    ComboBoxModifiedAfter.Reset();
                    ComboBoxModifiedBefore.Reset();
                }
                else
                {
                    FilterPanel.Visibility = Visibility.Collapsed;

                    Parameters.CreatedAfter = DateTime.MinValue;
                    Parameters.CreatedBefore = DateTime.MaxValue;
                    CreatedByEmail = string.Empty;

                    Parameters.ModifiedAfter = DateTime.MinValue;
                    Parameters.ModifiedBefore = DateTime.MaxValue;
                    ModifiedByEmail = string.Empty;

                    FolderSiteRelativeUrl = string.Empty;
                }
            }
        }

        public DateTime CreatedAfterDateTime
        {
            get { return Parameters.CreatedAfter; }
            set { Parameters.CreatedAfter = value; }
        }

        public DateTime CreatedBeforeDateTime
        {
            get { return Parameters.CreatedBefore; }
            set { Parameters.CreatedBefore = value; }
        }

        public string CreatedByEmail
        {
            get { return Parameters.CreatedByEmail; }
            set
            {
                Parameters.CreatedByEmail = value;
                OnPropertyChanged();
            }
        }


        public DateTime ModifiedAfterDateTime
        {
            get { return Parameters.ModifiedAfter; }
            set { Parameters.ModifiedAfter = value; }
        }

        public DateTime ModifiedBeforeDateTime
        {
            get { return Parameters.ModifiedBefore; }
            set { Parameters.ModifiedBefore = value; }
        }

        public string ModifiedByEmail
        {
            get { return Parameters.ModifiedByEmail; }
            set
            {
                Parameters.ModifiedByEmail = value;
                OnPropertyChanged();
            }
        }


        public string FolderSiteRelativeUrl
        {
            get { return Parameters.FolderSiteRelativeUrl; }
            set
            {
                Parameters.FolderSiteRelativeUrl = value;
                OnPropertyChanged();
            }
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
                    FilterTitleLabel.Title = "Item filter";
                    AllButton.Content = "All Items";
                }
                else if (value == "Library")
                {
                    FilterTitleLabel.Title = "File filter";
                    AllButton.Content = "All files";
                }
                else
                {
                    FilterTitleLabel.Title = "Files and Items filter";
                    AllButton.Content = "All files and items";
                }
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
