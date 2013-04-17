using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
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
        public List<string> SourceDatabases { get; set; }
        public ObservableCollection<CheckedListItem> ObservTables = new ObservableCollection<CheckedListItem>();

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.Source = new ConnectionConfig();
            this.Destination = new ConnectionConfig();
            this.SourceDatabases = new List<string>();
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
                        Properties.Settings.Default.DestinationServer = Destination.Server;
                        Properties.Settings.Default.DestinationUserId = Destination.UserId;
                        Properties.Settings.Default.DestinationPassword = Destination.Password;
                        Properties.Settings.Default.DestinationLoginSecurity = Destination.LoginSecurity;
                        Properties.Settings.Default.DestinationIsRemember = Destination.IsRemember;
                        Properties.Settings.Default.Save();
                        break;
                }
            }
        }
        private List<string> LoadDatabases(string connstring)
        {
            List<string> databases = new List<string>();
            using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(connstring))
            {
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    throw new InvalidOperationException("Data could not be read", exp);
                }
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                //cmd.CommandType = CommandType.StoredProcedure;
                //cmd.CommandText = "sp_databases";
                cmd.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4";
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    databases.Add(dr[0].ToString());
                }
            }

            return databases;
        }
        private void LoadTables(string connstring)
        {
            using (SqlConnection conn = new System.Data.SqlClient.SqlConnection(connstring))
            {
                try
                {
                    conn.Open();
                }
                catch (SqlException exp)
                {
                    throw new InvalidOperationException("Data could not be read", exp);
                }
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT name FROM sys.tables WHERE is_ms_shipped = 0";
                SqlDataReader dr = cmd.ExecuteReader();
                ObservTables.Clear();
                while (dr.Read())
                {
                    ObservTables.Add(new CheckedListItem { Name = dr[0].ToString(), IsChecked = false });
                }
            }
        }
        private void SourceRemember_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SourceIsRemember = Source.IsRemember;
            if (!Source.IsRemember)
            {
                Properties.Settings.Default.SourceServer = "";
                Properties.Settings.Default.SourceUserId = "";
                Properties.Settings.Default.SourcePassword = "";
                Properties.Settings.Default.SourceLoginSecurity = false;
            }
            Properties.Settings.Default.Save();
        }
        private void DestinationRemember_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DestinationIsRemember = Destination.IsRemember;
            if (!Destination.IsRemember)
            {
                Properties.Settings.Default.DestinationServer = "";
                Properties.Settings.Default.DestinationUserId = "";
                Properties.Settings.Default.DestinationPassword = "";
                Properties.Settings.Default.DestinationLoginSecurity = false;
            }
            Properties.Settings.Default.Save();
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
                    if (!Source.IsValidate)
                    {
                        int time = 0;
                        while (!Source.RunValidateConnection() && time < 1)
                        {
                            time++;
                        }
                        if (!Source.IsValidate)
                        {
                            e.Cancel = true;
                            imgSourceStatus.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            SourceDatabases = LoadDatabases(Source.ConnectionString());
                        }
                    }
                    else
                    {
                        imgSourceStatus.Visibility = System.Windows.Visibility.Hidden;
                        SourceDatabases = LoadDatabases(Source.ConnectionString());
                    }
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
            // BackgroundWorker bgw = sender as BackgroundWorker;
            // bgw.ReportProgress(100);
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

            if (conn.IsRemember)
            {
                SavePorperties(conn);
            }
        }
        private void bgwValidateConnection_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }
        #endregion
    }
}
