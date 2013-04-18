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

namespace NDBackuper
{
    public class Backuper : INotifyPropertyChanged
    {
        private int progress = 0;
        protected List<string> TablesOfBackup { get; set; }
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
            this.TablesOfBackup   = new List<string>();
        }

        #region Method

        public void RunBackup(List<string> backupTables)
        {
            // TODO: 執行緒晚點再移
            BackgroundWorker bgw      = new BackgroundWorker();
            bgw.DoWork               += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted   += bgwValidateConnection_RunWorkerCompleted;
            bgw.ProgressChanged      += bgwValidateConnection_ProgressChanged;
            bgw.WorkerReportsProgress = true;

            // TODO: Backup Here
            // DONE: 1.   Check destination database exsits.
            Smo.Server srvDestination = new Smo.Server(Destination.ServerConnection);
            if (!srvDestination.Databases.Contains(Destination.Database))
            {
                Smo.Database newdb = new Smo.Database(srvDestination, Destination.Database);
                newdb.Create();
                Smo.Server srvSource                      = new Smo.Server(Source.ServerConnection);
                Smo.Database dbSource                     = srvSource.Databases[Source.Database];
                Smo.Transfer transfer                     = new Smo.Transfer(dbSource);
                transfer.CopyAllUsers                     = true;
                transfer.CopyAllObjects                   = true;
                transfer.CopyData                         = true;
                transfer.CopySchema                       = true;
                transfer.CopyAllTables                    = false;
                transfer.Options.WithDependencies         = true;
                transfer.Options.DriAll                   = true;
                transfer.Options.ContinueScriptingOnError = false;
                foreach (string tbname in backupTables)
                {
                    transfer.ObjectList.Add(dbSource.Tables[tbname]);
                }
                transfer.DestinationServer      = Destination.Server;
                transfer.DestinationDatabase    = newdb.Name;
                transfer.DestinationLoginSecure = Destination.LoginSecurity;
                if (!Destination.LoginSecurity)
                {
                    transfer.DestinationLogin = Destination.UserId;
                    transfer.DestinationPassword = Destination.Password;
                }
                transfer.TransferData();
            }

            // TODO: 2.   If No date range, use transfer copy all database.
            // TODO: 2-1. If use date range, DataTable.Select(); filter Jobs key (klKey) and filter another table has FK by Jobs (fkJobKey, klJobKey)
            // TODO: 2-2. Use Sqlbulk copy datatable
            // TODO: 3.   If YES  Check db version
            // TODO: 3-1. Source > Destination => upgrade scripts
            // TODO: 3-2. Source == Destination => Run step 4 for merge data.
            // TODO: 3-3. Source < Destination => false; alert message and block;
            // TODO: 4.   Deal table releationship PK/FK to create DataSet & Datatable from Source. 
            // TODO: 5.   If table of ObservTable selected get record of Destination last PK int.
            //            List<Record> Record.LastKey, Record.TableName, Record.PKColumnName
            // TODO: 6.   DataSet.Fill(); get data and filter date range.
            // TODO: 7.   Use Sqlbulk copy datatable.

        }
        
        public bool CheckVersion()
        {
            var x = DbHelper.ReadOne(Source.ConnectionString(),
                             "Select TOP 1 * From WebDBVersion Order By klKey DESC");
            var y = DbHelper.ReadOne(Destination.ConnectionString(),
                             "Select TOP 1 * From WebDBVersion Order By klKey DESC");
            return false;
        }
        #endregion

        #region Threads
        public void bgwValidateConnection_DoWorkHandler(object sender, DoWorkEventArgs e)
        {
            ConnectionConfig conn = e.Argument as ConnectionConfig;
            conn.RunValidateConnection();
            e.Result = conn;
            BackgroundWorker bgw = sender as BackgroundWorker;
            bgw.ReportProgress(100);
        }
        private void bgwValidateConnection_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
        private void bgwValidateConnection_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }
        #endregion

    }
}
