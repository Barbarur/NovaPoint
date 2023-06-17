using NovaPointLibrary.Commands.Utilities;
using NovaPointWPF.Pages;
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

namespace NovaPointWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public Page MainPage = new MainPage();

        public MainWindow()
        {
            InitializeComponent();

            MainWindowMainFrame.Content = MainPage;


            IsUpdated().
                ContinueWith(t => Console.WriteLine(t.Exception),TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task IsUpdated()
        {
            await Task.Run(async() =>
            {
                Properties.Settings.Default.IsUpdated = await VersionControl.IsUpdated();
                Properties.Settings.Default.Save();
            });
        }
    }
}
