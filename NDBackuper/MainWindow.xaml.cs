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
        public ObservableCollection<CheckedListItem> ObservTables { get; set; }

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.Source = new ConnectionConfig();
            this.Destination = new ConnectionConfig();
            this.SourceDatabases = new List<string>();
            this.ObservTables = new ObservableCollection<CheckedListItem>();
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

        #region Events
        #region Page-1
        // 驗證 Source Connection
        protected void btnSourceConnValidation_Click(object sender, RoutedEventArgs e)
        {
            Source.Database = "";
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Source);
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
        #endregion

       
       
        #region Page-2
        // P2-1: Combobox Select Event
        private void SourceDatabases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo != null && combo.SelectedIndex > 0 )
            {
                Source.Database = combo.SelectedItem.ToString();
                LoadTables(Source.ConnectionString());
            }
            else
            {
                ObservTables.Clear();
            }

        }
        #endregion

        #region Page-3
        // 驗證 Destination Connection
        protected void btnDestinationConnValidation_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Destination);
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

        #region Page-4
        
        #endregion

        #region Page-5
        
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
                    Source.Database = "";
                    Source.RunValidateConnection();
                    if (!Source.IsValidate)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        imgSourceStatus.Visibility = System.Windows.Visibility.Hidden;
                        SavePorperties(Source);
                        SourceDatabases = LoadDatabases(Source.ConnectionString());
                        cmbSourceDatabases.SelectedIndex = 0;
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
        #endregion

        #region DRY
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
            databases.Add("=== Select Database ===");
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

                List<string> necessaryTable = new List<string>();
                necessaryTable.Add("MCS");
                necessaryTable.Add("MCSProperty");
                necessaryTable.Add("Jobs");
                necessaryTable.Add("WebDBVersion");

                while (dr.Read())
                {
                    bool isChecked = false;
                    bool isEnable = true;
                    if (necessaryTable.Contains(dr[0].ToString()))
                    {
                        isChecked = true;
                        isEnable = false;
                    }

                    ObservTables.Add(new CheckedListItem { Name = dr[0].ToString(), IsChecked = isChecked, IsEnable = isEnable });
                }
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
