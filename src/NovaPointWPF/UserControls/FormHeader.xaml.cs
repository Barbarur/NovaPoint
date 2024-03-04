using NovaPointWPF.Pages.DesignMaterial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for FormHeader.xaml
    /// </summary>
    public partial class FormHeader : UserControl
    {
        public string SolutionTitle
        {
            get { return (string)GetValue(SolutionTitleProperty); }
            set { SetValue(SolutionTitleProperty, value); }
        }

        public static readonly DependencyProperty SolutionTitleProperty =
            DependencyProperty.Register("SolutionTitle", typeof(string), typeof(FormHeader),
                new PropertyMetadata("Solution"));


        public string SolutionCode
        {
            get { return (string)GetValue(SolutionCodeProperty); }
            set { SetValue(SolutionCodeProperty, value); }
        }

        public static readonly DependencyProperty SolutionCodeProperty =
            DependencyProperty.Register("SolutionCode", typeof(string), typeof(FormHeader),
                new PropertyMetadata(string.Empty));

        public string SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki";

        private void GoToDocumentation(object sender, RoutedEventArgs e)
        {
            var url = SolutionDocs.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void RedTheDocsClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {SolutionDocs}") { CreateNoWindow = true });
        }


        public FormHeader()
        {
            InitializeComponent();
        }
    }
}
