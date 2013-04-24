using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
        public Backuper BackupObject { get; set; }
        public ObservableCollection<CheckedListItem> ObservTables { get; set; }

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.Source          = new ConnectionConfig();
            this.Destination     = new ConnectionConfig();
            this.SourceDatabases = new List<string>();
            this.ObservTables    = new ObservableCollection<CheckedListItem>();
            this.BackupObject    = new Backuper(Source, Destination);

            // Load properties
            Source.Name          = "Source";
            Source.Server        = Properties.Settings.Default.SourceServer;
            Source.UserId        = Properties.Settings.Default.SourceUserId;
            Source.Password      = Properties.Settings.Default.SourcePassword;
            Source.LoginSecurity = Properties.Settings.Default.SourceLoginSecurity;
            Source.IsRemember    = Properties.Settings.Default.SourceIsRemember;

            Destination.Name          = "Destination";
            Destination.Server        = Properties.Settings.Default.DestinationServer;
            Destination.UserId        = Properties.Settings.Default.DestinationUserId;
            Destination.Password      = Properties.Settings.Default.DestinationPassword;
            Destination.LoginSecurity = Properties.Settings.Default.DestinationLoginSecurity;
            Destination.IsRemember    = Properties.Settings.Default.DestinationIsRemember;

            this.DataContext = this;

        }
        #endregion

        #region Events
        #region Page-1
        // 本地驗證時清空帳號密碼
        private void chkSourceLoginSecurity_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SourceLoginSecurity = Source.LoginSecurity;
            if (Source.LoginSecurity)
            {
                Properties.Settings.Default.SourceServer = Source.Server;
                Properties.Settings.Default.SourceUserId = "";
                Properties.Settings.Default.SourcePassword = "";
            }
            Properties.Settings.Default.Save();
        }
        // 驗證 Source Connection
        protected void btnSourceConnValidation_Click(object sender, RoutedEventArgs e)
        {
            // Reset
            imgSourceStatus.Visibility = System.Windows.Visibility.Hidden;
            btnSourceConnValidation.IsEnabled = false;
            Source.Database = "";

            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Source);
        }
        // 切換記憶密碼
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
        // 本地驗證時清空帳號密碼
        private void chkDestinationLoginSecurity_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DestinationLoginSecurity = Destination.LoginSecurity;
            if (Source.LoginSecurity)
            {
                Properties.Settings.Default.DestinationServer = Destination.Server;
                Properties.Settings.Default.DestinationUserId = "";
                Properties.Settings.Default.DestinationPassword = "";
            }
            Properties.Settings.Default.Save();
        }
        // 驗證 Destination Connection
        protected void btnDestinationConnValidation_Click(object sender, RoutedEventArgs e)
        {
            // Reset
            imgDestinationStatus.Visibility = System.Windows.Visibility.Hidden;
            btnDestinationConnValidation.IsEnabled = false;
            Destination.Database = "";

            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(Destination);
        }
        // 切換記憶密碼
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
        private void chkUseDateRange_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkUseDateRange.IsChecked)
            {
                lbFromTag.Visibility = System.Windows.Visibility.Visible;
                dpFrom.Visibility = System.Windows.Visibility.Visible;
                lbTo.Visibility = System.Windows.Visibility.Visible;
                dpTo.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                lbFromTag.Visibility = System.Windows.Visibility.Collapsed;
                dpFrom.Visibility = System.Windows.Visibility.Collapsed;
                lbTo.Visibility = System.Windows.Visibility.Collapsed;
                dpTo.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        #endregion

        #region Page-5
        protected void btnRunBackup_Click(object sender, RoutedEventArgs e)
        {
            // DONE: accomplist testing
            List<CheckedListItem> backupTables = ObservTables.Where(o => o.IsChecked == true).ToList();
            BackupObject.IsDateFiltration = (bool)chkUseDateRange.IsChecked;
            if (BackupObject.IsDateFiltration)
            {
                BackupObject.DateFrom = (DateTime)dpFrom.SelectedDate;
                BackupObject.DateTo = (DateTime)dpTo.SelectedDate;
            }
            // TODO: review code here.
            BackupObject.Log += "Starting..." + Environment.NewLine;
            BackupObject.RunBackup(backupTables);
        }

        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtLog.ScrollToEnd();
        }
        #endregion
        #endregion

        #region Wizard Events
        // Close
        private void wzdMain_Cancelled(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void wzdMain_Finished(object sender, RoutedEventArgs e)
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
                        if (Source.IsRemember)
                        {
                            SavePorperties(Source);
                        }
                        SourceDatabases = LoadDatabases(Source.ConnectionString());
                        cmbSourceDatabases.ItemsSource = SourceDatabases;
                        cmbSourceDatabases.SelectedIndex = 0;
                    }
                    break;
                case "wzdPage2":
                    if (ObservTables.Where(t => t.IsChecked == true).Count() < 1)
                    {
                        MessageBox.Show("Please select at least one table.");
                        e.Cancel = true;
                    }
                    break;
                case "wzdPage3":
                    Destination.Database = "";
                    Destination.RunValidateConnection();
                    if (!Destination.IsValidate)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        imgDestinationStatus.Visibility = System.Windows.Visibility.Hidden;
                        if (Destination.IsRemember)
                        {
                            SavePorperties(Destination);
                        }
                        if (txtDestinationDatabaseName.Text.Trim() == "")
                        {
                            txtDestinationDatabaseName.Text = Source.Database;
                        }
                    }
                    break;
                case "wzdPage4":
                    if (String.IsNullOrEmpty(txtDestinationDatabaseName.Text))
                    {
                        MessageBox.Show("Please input database name for backup");
                        e.Cancel = true;
                    }
                    else
                    {
                        Destination.Database = txtDestinationDatabaseName.Text;
                        if (String.IsNullOrEmpty(Destination.Database))
                        {
                            MessageBox.Show("Database name format error!");
                            e.Cancel = true;
                        }
                    }
                    if ((bool)chkUseDateRange.IsChecked)
                    {
                        if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
                        {
                            MessageBox.Show("Please select date range");
                            e.Cancel = true;
                        }
                    }
                    break;
                case "wzdPage5":
                    break;
            }
        }

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

                List<string> overrideTable = new List<string>();
                overrideTable.Add("WebDBVersion");
                overrideTable.Add("UserSlitParams");
                overrideTable.Add("UserTerms");
                overrideTable.Add("UserROIParams");
                overrideTable.Add("UserValues");
                overrideTable.Add("UserJobParams");
                overrideTable.Add("UserCutParams");

                while (dr.Read())
                {
                    bool isChecked = false;
                    bool isEnable = true;
                    bool isOverride = false;
                    if (necessaryTable.Contains(dr[0].ToString()))
                    {
                        isChecked = true;
                        isEnable = false;
                    }
                    if (overrideTable.Contains(dr[0].ToString()))
                    {
                        isOverride = true;
                    }

                    ObservTables.Add(new CheckedListItem { 
                        Name = dr[0].ToString(), 
                        IsChecked = isChecked, 
                        IsEnable = isEnable,
                        IsOverride = isOverride
                    });
                }
            }
        }

#if DEBUG
        public void ShowSchema(DataTable dt)
        {
            // PK
            Console.WriteLine();
            System.Diagnostics.Debug.WriteLine("PrimaryKey:"+dt.PrimaryKey.Length.ToString());
            foreach (DataColumn c in dt.PrimaryKey)
                System.Diagnostics.Debug.WriteLine(c.ColumnName);

            // Constraint
            System.Diagnostics.Debug.WriteLine("Constraints:" + dt.Constraints.Count.ToString());
            foreach (Constraint c in dt.Constraints)
                System.Diagnostics.Debug.WriteLine(c.ConstraintName);

            System.Diagnostics.Debug.WriteLine("ParentRelations:" + dt.ParentRelations.Count.ToString());
            System.Diagnostics.Debug.WriteLine("ChildRelations:" + dt.ChildRelations.Count.ToString());
            System.Diagnostics.Debug.WriteLine("-----------------------");
        }
#endif

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
                    btnSourceConnValidation.IsEnabled = true;
                    imgSourceStatus.Visibility = System.Windows.Visibility.Visible;
                    break;
                case "Destination":
                    btnDestinationConnValidation.IsEnabled = true;
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
