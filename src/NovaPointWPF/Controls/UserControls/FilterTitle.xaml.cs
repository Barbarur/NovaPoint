using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Controls.UserControls
{
    public partial class FilterTitle : UserControl, INotifyPropertyChanged
    {

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(FilterTitle),
            new FrameworkPropertyMetadata(
                defaultValue: "Filter",
                flags: FrameworkPropertyMetadataOptions.AffectsMeasure));


        public string LearnMoreLink { get; set; } = "https://github.com/Barbarur/NovaPoint/wiki";

        public FilterTitle()
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
            Process.Start(new ProcessStartInfo("cmd", $"/c start {LearnMoreLink}") { CreateNoWindow = true });
        }
    }
}
