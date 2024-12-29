using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Solutions.Report;
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
    /// Interaction logic for SiteDetailsForm.xaml
    /// </summary>
    public partial class SiteDetailsForm : UserControl, INotifyPropertyChanged
    {
        public SiteInformationParameters Parameters { get; set; } = new();

        private bool _basicReport = true;
        public bool BasicReport
        {
            get { return _basicReport; }
            set
            {
                _basicReport = value;
                OnPropertyChanged();

                if (value)
                {
                    IncludeHubInfo = false;
                    IncludeClassification = false;
                    IncludeSharingLinks = false;
                    IncludePrivacy = false;
                }
            }
        }

        private bool _includeHubInfo = false;
        public bool IncludeHubInfo
        {
            get { return _includeHubInfo; }
            set
            {
                _includeHubInfo = value;
                Parameters.IncludeHubInfo = value;
                OnPropertyChanged();

                if (value) { BasicReport = false; }
            }
        }

        private bool _includeClassification = false;
        public bool IncludeClassification
        {
            get { return _includeClassification; }
            set
            {
                _includeClassification = value;
                Parameters.IncludeClassification = value;
                OnPropertyChanged();
                
                if (value) { BasicReport = false; }
            }
        }

        private bool _includeSharingLinks = false;
        public bool IncludeSharingLinks
        {
            get { return _includeSharingLinks; }
            set
            {
                _includeSharingLinks = value;
                Parameters.IncludeSharingLinks = value;
                OnPropertyChanged();

                if (value) { BasicReport = false; }
            }
        }

        private bool _includePrivacy = false;
        public bool IncludePrivacy
        {
            get { return _includePrivacy; }
            set
            {
                _includePrivacy = value;
                Parameters.IncludePrivacy = value;
                OnPropertyChanged();

                if (value) { BasicReport = false; }
            }
        }


        public SiteDetailsForm()
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
