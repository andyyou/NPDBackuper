using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace NDBackuper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public ConnectionConfig Source { get; set; }
        public ConnectionConfig Destination { get; set; }
        public ObservableCollection<CheckedListItem> ObservTables = new ObservableCollection<CheckedListItem>();

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.Source = new ConnectionConfig();
            this.Destination = new ConnectionConfig();
            Source.Server = @"(localdb)\v11.0";
            Source.UserId = "andy";
            Source.Password = "xx";
            Source.LoginSecurity = true;
            Source.IsRemember = true;

            Destination.Server = "192.168.100.248";
            Destination.UserId = "apputu";
            Destination.Password = "oooo";
            Destination.LoginSecurity = false;
            Destination.IsRemember = true;
            
            this.DataContext = this;

        }
        #endregion
        
        #region Wizard Events
        // Close
        private void wzdMain_Cancelled(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Wizard_Commit(object sender, AvalonWizard.WizardPageConfirmEventArgs e)
        {

        }

        #endregion
    }
}
