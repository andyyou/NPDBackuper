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
            // Load properties
            Source.Name = "Source";
            Source.Server = Properties.Settings.Default.SourceServer;
            Source.UserId = Properties.Settings.Default.SourceUserId;
            Source.Password = Properties.Settings.Default.SourcePassword;
            Source.LoginSecurity = Properties.Settings.Default.SourceLoginSecurity;
            Source.IsRemember = Properties.Settings.Default.SourceIsRemember;

            Destination.Name = "Destination";
            Destination.Server = Properties.Settings.Default.DestinationServer;
            Destination.UserId = Properties.Settings.Default.DestinationUserId;
            Destination.Password = Properties.Settings.Default.DestinationPassword;
            Destination.LoginSecurity = Properties.Settings.Default.DestinationLoginSecurity;
            Destination.IsRemember = Properties.Settings.Default.DestinationIsRemember;
            
            this.DataContext = this;

        }
        #endregion
        #region Controls Events
        // 驗證 Source Connection
        protected void btnSourceConnValidation_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Source);
        }
        // 驗證 Destination Connection
        protected void btnDestinationConnValidation_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Destination);
        }
        protected void SavePorperties(ConnectionConfig conn)
        {
            if (conn.IsValidate)
            {
                switch (conn.Name)
                {
                    case "Source":
                        Properties.Settings.Default.SourceServer = Source.Server;
                        Properties.Settings.Default.SourceUserId = Source.UserId;
                        Properties.Settings.Default.SourcePassword = Source.Password;
                        Properties.Settings.Default.SourceLoginSecurity = Source.LoginSecurity;
                        Properties.Settings.Default.SourceIsRemember = Source.IsRemember;
                        Properties.Settings.Default.Save();
                        break;
                    case "Destination":
                        Properties.Settings.Default.SourceServer = Source.Server;
                        Properties.Settings.Default.SourceUserId = Source.UserId;
                        Properties.Settings.Default.SourcePassword = Source.Password;
                        Properties.Settings.Default.SourceLoginSecurity = Source.LoginSecurity;
                        Properties.Settings.Default.SourceIsRemember = Source.IsRemember;
                        Properties.Settings.Default.Save();
                        break;
                }
            }
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
            switch (e.Page.Name)
            {
                case "wzdPage1":

                    break;
                case "wzdPage2":
                    break;
                case "wzdPage3":
                    break;
                case "wzdPage4":
                    break;
                case "wzdPage5":
                    break;
            }
        }

        #endregion

        #region Threads
        public void bgwValidateConnection_DoWorkHandler(object sender, DoWorkEventArgs e)
        {
            ConnectionConfig conn = e.Argument as ConnectionConfig;
            conn.RunValidateConnection();
            e.Result = conn;
        }
        private void bgwValidateConnection_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ConnectionConfig conn = e.Result as ConnectionConfig;
            switch (conn.Name)
            { 
                case "Source":
                    imgSourceStatus.Visibility = System.Windows.Visibility.Visible;
                    break;
                case "Destination":
                    imgDestinationStatus.Visibility = System.Windows.Visibility.Visible;
                    break;
            }
            SavePorperties(conn);
        }
        #endregion
    }
}
