using NovaPointLibrary.Commands.SharePoint.Item;
using System;
using System.ComponentModel;
using System.Diagnostics;
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
                }
                else
                {
                    FilterPanel.Visibility = Visibility.Collapsed;
                    CreatedAfter = false;
                    CreatedBefore = false;
                    CreatedBy = false;

                    ModifiedAfter = false;
                    ModifiedBefore = false;
                    ModifiedBy = false;

                    FolderPath = false;
                }
            }
        }


        private bool _createdAfter = false;
        public bool CreatedAfter
        {
            get { return _createdAfter; }
            set
            {
                _createdAfter = value;
                if (value)
                {
                    ComboBoxCreatedAfter.Visibility = Visibility.Visible;
                    ComboBoxCreatedAfter.Reset();
                }
                else
                {
                    ComboBoxCreatedAfter.Visibility = Visibility.Collapsed;
                    Parameters.CreatedAfter = DateTime.MinValue;
                }
                OnPropertyChanged();
            }
        }
        public DateTime CreatedAfterDateTime
        {
            get { return Parameters.CreatedAfter; }
            set
            {
                Parameters.CreatedAfter = value;
                OnPropertyChanged();
            }
        }

        private bool _createdBefore = false;
        public bool CreatedBefore
        {
            get { return _createdBefore; }
            set
            {
                _createdBefore = value;
                if (value)
                {
                    ComboBoxCreatedBefore.Visibility = Visibility.Visible;
                    ComboBoxCreatedBefore.Reset();
                }
                else
                {
                    ComboBoxCreatedBefore.Visibility = Visibility.Collapsed;
                    Parameters.CreatedBefore = DateTime.MaxValue;
                }
                OnPropertyChanged();
            }
        }
        public DateTime CreatedBeforeDateTime
        {
            get { return Parameters.CreatedBefore; }
            set
            {
                Parameters.CreatedBefore = value;
            }
        }

        private bool _createdBy = false;
        public bool CreatedBy
        {
            get { return _createdBy; }
            set
            {
                _createdBy = value;
                if (value)
                {
                    TextBoxCreatedBy.Visibility = Visibility.Visible;
                }
                else
                {
                    TextBoxCreatedBy.Visibility = Visibility.Collapsed;
                    CreatedByEmail = string.Empty;
                }
                OnPropertyChanged();
            }
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


        private bool _modifiedAfter = false;
        public bool ModifiedAfter
        {
            get { return _modifiedAfter; }
            set
            {
                _modifiedAfter = value;
                if (value)
                {
                    ComboBoxModifiedAfter.Visibility = Visibility.Visible;
                    ComboBoxModifiedAfter.Reset();
                }
                else
                {
                    ComboBoxModifiedAfter.Visibility = Visibility.Collapsed;
                    Parameters.ModifiedAfter = DateTime.MinValue;
                }
                OnPropertyChanged();
            }
        }
        public DateTime ModifiedAfterDateTime
        {
            get { return Parameters.ModifiedAfter; }
            set { Parameters.ModifiedAfter = value; }
        }

        private bool _modifiedBefore = false;
        public bool ModifiedBefore
        {
            get { return _modifiedBefore; }
            set
            {
                _modifiedBefore = value;
                if (value)
                {
                    ComboBoxModifiedBefore.Visibility = Visibility.Visible;
                    ComboBoxModifiedBefore.Reset();
                }
                else
                {
                    ComboBoxModifiedBefore.Visibility = Visibility.Collapsed;
                    Parameters.ModifiedBefore = DateTime.MaxValue;
                }
                OnPropertyChanged();
            }

        }
        public DateTime ModifiedBeforeDateTime
        {
            get { return Parameters.ModifiedBefore; }
            set { Parameters.ModifiedBefore = value; }
        }
        private bool _modifiedBy = false;
        public bool ModifiedBy
        {
            get { return _modifiedBy; }
            set
            {
                _modifiedBy = value;
                if (value)
                {
                    TextBoxModifiedBy.Visibility = Visibility.Visible;
                }
                else
                {
                    TextBoxModifiedBy.Visibility = Visibility.Collapsed;
                    ModifiedByEmail = string.Empty;
                }
                    OnPropertyChanged();
            }
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


        private bool _folderPath = false;
        public bool FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                if (value)
                {
                    PanelFolderPath.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelFolderPath.Visibility = Visibility.Collapsed;
                    FolderSiteRelativeUrl = string.Empty;
                }
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



        public string DefinitionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Definitions-files-and-items-filter";


        public ItemForm()
        {
            InitializeComponent();

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void ReadTheDocsClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {DefinitionDocs}") { CreateNoWindow = true });
        }

    }
}
