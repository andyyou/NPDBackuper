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
        private string log = "Ready to run backup." + Environment.NewLine;
        public ConnectionConfig Source { get; set; }
        public ConnectionConfig Destination { get; set; }
        public bool IsCompleted { get; set; }
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
            this.IsCompleted      = false;
            this.IsDateFiltration = false;
        }

        #region Method

        public void RunBackup(List<string> backupTables)
        {
            // Set progress to zero before run backup
            this.Progress = 0;
            // TODO: 執行緒晚點再移
            BackgroundWorker bgw      = new BackgroundWorker();
            bgw.DoWork               += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted   += bgwValidateConnection_RunWorkerCompleted;
            bgw.ProgressChanged      += bgwValidateConnection_ProgressChanged;
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerAsync(backupTables);
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
            int sourceVer = (int)(DbHelper.ReadOne(Source.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));
            int destinationVer = (int)(DbHelper.ReadOne(Destination.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));

            if (sourceVer - destinationVer > 0)
            {
                return UpgradeDatabase(sourceVer, destinationVer);
            }
            else if (sourceVer == destinationVer)
            {
                return true;
            }
            else
            {
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
                }
            }

            return false;
        }
        #endregion

        #region Threads
        public void bgwValidateConnection_DoWorkHandler(object sender, DoWorkEventArgs e)
        {
            // TODO: Backup Here
            List<string> backupTables = e.Argument as List<string>;
            Smo.Server srvDestination = new Smo.Server(Destination.ServerConnection);
            if (!srvDestination.Databases.Contains(Destination.Database))
            {
                //if (!IsDateFiltration)
                //{
                //    #region Copy All Database and Table to another Server
                //    Smo.Database newdb = new Smo.Database(srvDestination, Destination.Database);
                //    newdb.Create();
                //    Smo.Server srvSource = new Smo.Server(Source.ServerConnection);
                //    Smo.Database dbSource = srvSource.Databases[Source.Database];
                //    Smo.Transfer transfer = new Smo.Transfer(dbSource);
                //    transfer.CopyAllUsers = true;
                //    transfer.CopyAllObjects = false;
                //    transfer.CopyAllTables = false;
                //    transfer.CopyData = true;
                //    transfer.CopySchema = true;
                //    transfer.Options.WithDependencies = true;
                //    transfer.Options.DriAll = true;
                //    transfer.DataTransferEvent += new DataTransferEventHandler(DataTransferHandler);
                //    transfer.Options.ContinueScriptingOnError = false;
                //    // Create all table when database not exist
                //    foreach (var tbl in dbSource.Tables)
                //    {
                //        transfer.ObjectList.Add(tbl);
                //    }
                //    transfer.DestinationServer = Destination.Server;
                //    transfer.DestinationDatabase = newdb.Name;
                //    transfer.DestinationLoginSecure = Destination.LoginSecurity;
                //    if (!Destination.LoginSecurity)
                //    {
                //        transfer.DestinationLogin = Destination.UserId;
                //        transfer.DestinationPassword = Destination.Password;
                //    }
                //    transfer.TransferData();
                //    #endregion
                //}
                //else
                //{
                    #region Create Database and Copy Table sechma
                    Smo.Database newdb = new Smo.Database(srvDestination, Destination.Database);
                    newdb.Create();
                    Smo.Server srvSource = new Smo.Server(Source.ServerConnection);
                    Smo.Database dbSource = srvSource.Databases[Source.Database];
                    Smo.Transfer transfer = new Smo.Transfer(dbSource);
                    transfer.CopyAllUsers = true;
                    transfer.CopyAllObjects = false;
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
                    List<string> sortTables = DbHelper.Fill(Source.ConnectionString(), ds, backupTables);
                    ds.Tables["Jobs"].Rows.Cast<DataRow>().LastOrDefault().Delete(); // Always delete last record.
                    // Filter DateRange
                    if (IsDateFiltration)
                    {
                        ds.Tables["Jobs"].Rows.Cast<DataRow>().Where(j => (DateTime)j["Date"] < DateFrom || (DateTime)j["Date"] > DateTo).ToList().ForEach(j => j.Delete());
                        ds.Tables["Jobs"].AcceptChanges();
                    }

                    #endregion

                    #region Execute SqlBulk Copy
                    foreach (string tbl in sortTables)
                    {
                        DbHelper.ExecuteSqlBulk(Destination.ConnectionString(), ds.Tables[tbl]);
                        this.Progress += 100 / backupTables.Count;
                        this.Log += DbHelper.SqlBulkLog.LastOrDefault() + Environment.NewLine;
                    }
                    #endregion
                //}
            }
            else
            {
                if (CheckVersion())
                {
                    // TODO: now todo here.
                    #region Get Source Data and Filter date range
                    DataSet ds = DbHelper.CopySechmaFromDatabase(Source.ConnectionString());
                    List<string> sortTables = DbHelper.Fill(Source.ConnectionString(), ds, backupTables);
                    ds.Tables["Jobs"].Rows.Cast<DataRow>().LastOrDefault().Delete(); // Always delete last record.
                    if (IsDateFiltration)
                    {
                        ds.Tables["Jobs"].Rows.Cast<DataRow>().Where(j => (DateTime)j["Date"] < DateFrom || (DateTime)j["Date"] > DateTo).ToList().ForEach(j => j.Delete());
                    }
                    ds.Tables["Jobs"].AcceptChanges();
                    #endregion

                    #region Get destination PK list of table exists and modify for merge
                    foreach (string tbl in sortTables)
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
                    ds.AcceptChanges();
                    #endregion

                    #region Execute SqlBulk Copy
                    foreach (string tbl in sortTables)
                    {
                        DbHelper.ExecuteSqlBulk(Destination.ConnectionString(), ds.Tables[tbl]);
                        this.Progress += 100 / backupTables.Count;
                        this.Log += DbHelper.SqlBulkLog.LastOrDefault() + Environment.NewLine;
                    }
                    #endregion
                }
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
            // TODO: 4.   Deal table releationship PK/FK to create DataSet & Datatable from Source. 
            // TODO: 5.   If table of ObservTable selected get record of Destination last PK int.
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
            this.Log += "Backup Complete!!" + Environment.NewLine + Environment.NewLine;
            this.Progress = 100;
        }
        private void bgwValidateConnection_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }
        #endregion

    }
}
