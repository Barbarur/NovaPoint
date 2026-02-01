using NovaPointLibrary.Commands.Directory;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.SolutionsFormParameters
{

    public partial class DirectoryGroupParametersForm : UserControl, INotifyPropertyChanged
    {
        public DirectoryGroupParameters Parameters { get; set; } = new();

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

        public bool IncludeMS365
        {
            get { return Parameters.IncludeMS365; }
            set
            { Parameters.IncludeMS365 = value; }
        }

        public bool IncludeSecurity
        {
            get { return Parameters.IncludeSecurity; }
            set { Parameters.IncludeSecurity = value; }
        }

        public bool IncludeEmailSecurity
        {
            get { return Parameters.IncludeEmailSecurity; }
            set { Parameters.IncludeEmailSecurity = value; }
        }

        public bool IncludeDistributionList
        {
            get { return Parameters.IncludeDistributionList; }
            set { Parameters.IncludeDistributionList = value; }
        }

        public bool IncludeOwners
        {
            get { return Parameters.IncludeOwners; }
            set { Parameters.IncludeOwners = value; }
        }

        public bool IncludeMembersCount
        {
            get { return Parameters.IncludeMembersCount; }
            set { Parameters.IncludeMembersCount = value; }
        }


        public DirectoryGroupParametersForm()
        {
            InitializeComponent();
            ComboBoxCreatedAfter.Reset();
            ComboBoxCreatedBefore.Reset();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
