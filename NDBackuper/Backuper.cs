using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Source: http://msdn.microsoft.com/en-us/library/ms162129%28v=sql.105%29
using Smo = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.SqlEnum;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.ObjectModel;
using System.Data;

namespace NDBackuper
{
    public class Backuper : INotifyPropertyChanged
    {
        private int progress = 0;
        private bool iscomplete = true;
        private string log = "Ready to run backup." + Environment.NewLine;
        public ConnectionConfig Source { get; set; }
        public ConnectionConfig Destination { get; set; }
        
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsDateFiltration { get; set; }
        public int Progress
        {
            get { return progress; }
            set {
                if (value >= 0 && value <= 100)
                {
                    progress = value;
                    RaisePropertyChanged("Progress");
                }
            }
        }
        public string Log
        {
            get { return log; }
            set {
                log = value;
                RaisePropertyChanged("Log");
            }
        }
        public bool IsCompleted
        {
            get { return iscomplete; }
            set
            {
                iscomplete = value;
                RaisePropertyChanged("IsCompleted");
            }
        } 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged(String propertyName)
        {
            if ((PropertyChanged != null))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public Backuper(ConnectionConfig source, ConnectionConfig destination)
        {
            this.Source           = source;
            this.Destination      = destination;
            this.IsCompleted      = true;
            this.IsDateFiltration = false;
        }

        #region Method
        /// <summary>
        /// Execute backup database.
        /// </summary>
        /// <param name="backupTables">Select backup tables</param>
        public void RunBackup(List<CheckedListItem> backupTables)
        {
            // Set progress to zero before run backup
            this.Progress = 5;
            this.IsCompleted = false;
            // TODO: 執行緒晚點再移
            BackgroundWorker bgw      = new BackgroundWorker();
            bgw.DoWork               += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted   += bgwValidateConnection_RunWorkerCompleted;
            bgw.ProgressChanged      += bgwValidateConnection_ProgressChanged;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(backupTables);
        }
        /// <summary>
        /// Clear data for merge override table
        /// </summary>
        /// <param name="tables">override table</param>
        public void DeleteDestinationOverride(List<string> tables)
        {
            foreach(string table in tables)
            {
                bool exists = DbHelper.IsTableExists(Destination.ConnectionString(), table);
                if (exists)
                {
                    string sql = string.Format("Delete From {0}", table);
                    DbHelper.ExecuteSql(Destination.ConnectionString(), sql);
                }
            }
        }
        /// <summary>
        /// Compare source and destination database version
        /// 1. Source equal to destination : true
        /// 2. Source newer than destination, upgrade database and return upgrade result
        /// 3. Destination newer than source, stop backup task
        /// </summary>
        /// <returns>若來源版本相同或升級成功：true、來源版本較舊或升級失敗：false</returns>
        public bool CheckVersion()
        {
            bool sourceIsExists = DbHelper.IsTableExists(Source.ConnectionString(), "WebDBVersion");
            bool destinationIsExists = DbHelper.IsTableExists(Destination.ConnectionString(), "WebDBVersion");
            if (!sourceIsExists || !destinationIsExists)
            {
                this.Log += "Can't get source/destination database version." + Environment.NewLine;
                return false;
            }

            int sourceVer = (int)(DbHelper.ReadOne(Source.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));
            int destinationVer = (int)(DbHelper.ReadOne(Destination.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));

            if (sourceVer - destinationVer > 0)
            {
                System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Warning: Database upgrade operation has risk.\n\nAre you sure continue?", "Database Upgrade Confirm", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.OK)
                {
                    if (UpgradeDatabase(sourceVer, destinationVer))
                    {
                        return true;
                    }
                    else
                    {
                        this.Log += "An error occurred during upgrade database." + Environment.NewLine;
                        return false;
                    }
                }
                else
                {
                    this.Log += "Database upgrade cancelled by user." + Environment.NewLine;
                    return false;
                }
            }
            else if (sourceVer == destinationVer)
            {
                return true;
            }
            else
            {
                this.Log += "Destination database version is higher than source." + Environment.NewLine;
                return false;
            }
        }

        /// <summary>
        /// Upgrade database
        /// </summary>
        /// <returns>return upgrade result</returns>
        public bool UpgradeDatabase(int sourceVer, int destinationVer)
        {
            // Get installation directory from registry
            Microsoft.Win32.RegistryKey registry = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Wintriss Engineering\Web Inspector");
            if (registry != null)
            {
                if (registry.GetValueNames().Contains("INSTALLDIR"))
                {
                    try
                    {
                        string dbUtilityDir = registry.GetValue("INSTALLDIR").ToString() + @"\Database Utility\";
                        List<string> cmdPathList = new List<string>();

                        while (destinationVer <= sourceVer)
                        {
                            cmdPathList.AddRange(System.IO.Directory.GetFiles(dbUtilityDir)
                                .Where(f => f.Contains(string.Format("Upgrade_{0}", destinationVer)))
                                .ToList());
                            destinationVer++;
                        }

                        string conn = Destination.ConnectionString();
                        foreach (string path in cmdPathList)
                        {
                            DbHelper.ExecuteScript(conn, path);
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                }
            }

            return false;
        }
        #endregion

        #region Threads
        public void bgwValidateConnection_DoWorkHandler(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<CheckedListItem> backupTables = e.Argument as List<CheckedListItem>;
                Smo.Server srvDestination = new Smo.Server(Destination.ServerConnection);
                if (!srvDestination.Databases.Contains(Destination.Database))
                {
                    #region Create Database and Copy Table sechma
                    Smo.Database newdb = new Smo.Database(srvDestination, Destination.Database);
                    newdb.Create();
                    Smo.Server srvSource = new Smo.Server(Source.ServerConnection);
                    Smo.Database dbSource = srvSource.Databases[Source.Database];
                    Smo.Transfer transfer = new Smo.Transfer(dbSource);
                    transfer.CopyAllUsers = true;
                    transfer.CopyAllObjects = true;
                    transfer.CopyAllTables = false;
                    transfer.CopyData = false;
                    transfer.CopySchema = true;
                    transfer.Options.WithDependencies = true;
                    transfer.Options.DriAll = true;
                    transfer.Options.ContinueScriptingOnError = false;
                    // Create all table when database not exist
                    foreach (var tbl in dbSource.Tables)
                    {
                        transfer.ObjectList.Add(tbl);
                    }
                    transfer.DestinationServer = Destination.Server;
                    transfer.DestinationDatabase = newdb.Name;
                    transfer.DestinationLoginSecure = Destination.LoginSecurity;
                    if (!Destination.LoginSecurity)
                    {
                        transfer.DestinationLogin = Destination.UserId;
                        transfer.DestinationPassword = Destination.Password;
                    }
                    transfer.TransferData();
                    #endregion

                    #region Get Source Data and Filter
                    DataSet ds = DbHelper.CopySechmaFromDatabase(Source.ConnectionString());
                    List<string> sortTables = DbHelper.Fill(Source.ConnectionString(), ds, backupTables.Select(b => b.Name).ToList());
                    if (ds.Tables["Jobs"].Rows.Count > 0)
                    {
                        ds.Tables["Jobs"].Rows.Cast<DataRow>().LastOrDefault().Delete(); // Always delete last record.
                        ds.Tables["Jobs"].AcceptChanges();
                        if (IsDateFiltration)
                        {
                            ds.Tables["Jobs"].Rows.Cast<DataRow>().Where(j => (DateTime)j["Date"] < DateFrom || (DateTime)j["Date"] > DateTo).ToList().ForEach(j => j.Delete());
                        }
                        foreach (var tbl in sortTables)
                        {
                            ds.Tables[tbl].AcceptChanges();
                        }
                    }
                    #endregion

                    #region Execute SqlBulk Copy
                    foreach (string tbl in sortTables)
                    {
                        DbHelper.ExecuteSqlBulk(Destination.ConnectionString(), ds.Tables[tbl]);
                        this.Progress += 100 / backupTables.Count;
                        if (DbHelper.SqlBulkLog.Count > 0)
                        {
                            this.Log += DbHelper.SqlBulkLog.LastOrDefault() + Environment.NewLine;
                        }
                    }
                    #endregion
                }
                else
                {
                    if (CheckVersion())
                    {
                        // TODO: now todo here.
                        #region Get Source Data and Filter date range
                        DataSet ds = DbHelper.CopySechmaFromDatabase(Source.ConnectionString());
                        List<string> sortTables = DbHelper.Fill(Source.ConnectionString(), ds, backupTables.Select(b => b.Name).ToList());
                        if (ds.Tables["Jobs"].Rows.Count > 0)
                        {
                            ds.Tables["Jobs"].Rows.Cast<DataRow>().LastOrDefault().Delete(); // Always delete last record.
                            ds.Tables["Jobs"].AcceptChanges();
                            if (IsDateFiltration)
                            {
                                ds.Tables["Jobs"].Rows.Cast<DataRow>().Where(j => (DateTime)j["Date"] < DateFrom || (DateTime)j["Date"] > DateTo).ToList().ForEach(j => j.Delete());
                            }
                            foreach (var tbl in sortTables)
                            {
                                ds.Tables[tbl].AcceptChanges();
                            }
                        }
                        #endregion

                        #region Get destination PK list of table exists and modify for merge
                        // filter override table don't modify key
                        List<string> overrideTable = backupTables.Where(b => b.IsOverride == true).Select(b => b.Name).ToList();
                        foreach (string tbl in sortTables)
                        {
                            if (!overrideTable.Contains(tbl))
                            {
                                string keycolumn = DbHelper.PrimaryKeyColumn(Destination.ConnectionString(), tbl);
                                string sql = string.Format("Select TOP 1 {0} From {1} Order By {0} DESC", keycolumn, tbl);
                                int? lastkey = (int?)(DbHelper.ReadOne(Destination.ConnectionString(), sql));
                                if (lastkey != null)
                                {
                                    int newkey = (int)lastkey + 1;
                                    int row = ds.Tables[tbl].Rows.Count;
                                    ds.Tables[tbl].Columns[keycolumn].ReadOnly = false;
                                    for (int i = row - 1; i >= 0; i--)
                                    {
                                        ds.Tables[tbl].Rows[i][keycolumn] = newkey + i;
                                    }
                                    ds.Tables[tbl].AcceptChanges();
                                }
                            }
                        }
                        ds.AcceptChanges();
                        #endregion

                        #region Delete override table data
                        List<string> clearTable = backupTables.Where(b => b.IsOverride == true).Select(b => b.Name).ToList();
                        DeleteDestinationOverride(clearTable);
                        #endregion

                        #region Execute SqlBulk Copy
                        foreach (string tbl in sortTables)
                        {
                            DbHelper.ExecuteSqlBulk(Destination.ConnectionString(), ds.Tables[tbl]);
                            this.Progress += 100 / backupTables.Count;
                            if (DbHelper.SqlBulkLog.Count > 0)
                            {
                                this.Log += DbHelper.SqlBulkLog.LastOrDefault() + Environment.NewLine;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        this.Log += "*** Database upgrade failed ***" + Environment.NewLine;
                        e.Result = false;
                    }
                }
            }
            catch (Exception)
            {
                this.Log += "An error occurred during backup database." + Environment.NewLine;
                e.Result = false;
            }
            // DONE: 1.   Check destination database exsits.
            // DONE: 2.   If No date range, use transfer copy all database.
            // DONE: 2-1. If use date range, DataTable.Select(); filter Jobs key (klKey) and filter another table has FK by Jobs (fkJobKey, klJobKey)
            // DONE: 2-2. Use Sqlbulk copy datatable
            // DONE: 3.   If YES  Check db version
            // CheckVersion();
            // DONE: 3-1. Source > Destination => upgrade scripts
            // DONE: 3-2. Source == Destination => Run step 4 for merge data.
            // DONE: 3-3. Source < Destination => false; alert message and block;
            // DONE: 4.   Deal table releationship PK/FK to create DataSet & Datatable from Source. 
            // DONE: 5.   If table of ObservTable selected get record of Destination last PK int.
            //            List<Record> Record.LastKey, Record.TableName, Record.PKColumnName
            // TODO: 6.   DataSet.Fill(); get data and filter date range.
            // TODO: 7.   Use Sqlbulk copy datatable.
        }
        public void DataTransferHandler(object sender, DataTransferEventArgs e)
        {
            // Only show information message at log
            Smo.Transfer transfer = sender as Smo.Transfer;
            int percentage = 100;
            this.Progress += percentage / transfer.Database.Tables.Count;
            if (e.DataTransferEventType == DataTransferEventType.Information)
            {
                this.Log += (e.DataTransferEventType.ToString() + " : " + e.Message + Environment.NewLine);
            }
        }
        private void bgwValidateConnection_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null && (bool)e.Result == false)
            {
                this.Log += "Backup Abort!!" + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                this.Log += "Backup Complete!!" + Environment.NewLine + Environment.NewLine;
                this.Progress = 100;
            }
            this.IsCompleted = true;
        }
        private void bgwValidateConnection_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }
        #endregion

    }
}
