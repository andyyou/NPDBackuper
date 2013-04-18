using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.Source = source;
            this.Destination = destination;
            this.IsCompleted = false;
            this.IsDateFiltration = false;
            this.TablesOfBackup = new List<string>();
        }

        #region Method
        
        public void RunBackup()
        {
            // TODO: 執行緒晚點再移
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.ProgressChanged += bgwValidateConnection_ProgressChanged;
            bgw.WorkerReportsProgress = true;

            // TODO: Backup Here
            // TODO: 1.   Check destination database exsits.
            // TODO: 2.   If No, use transfer copy all database.
            // TODO: 2-1. If use date range, DataTable.Select(); filter Jobs key (klKey) and filter another table has FK by Jobs (fkJobKey, klJobKey)
            // TODO: 2-2. Use Sqlbulk copy datatable
            // TODO: 3.   If YES  Check db version
            // TODO: 3-1. Source > Destination => upgrade scripts
            // TODO: 3-2. Source == Destination => Run step 4 for merge data.
            // TODO: 3-3. Source < Destination => false; alert message and block;
            // TODO: 4.   Deal table releationship PK/FK to create DataSet & Datatable from Source. 
            // TODO: 5.   If table of ObservTable selected get record of Destination last PK int.
            //            List<Record> Record.LastKey, Record.TableName, Record.ColumnName
            // TODO: 6.   DataSet.Fill(); get data and filter date range.
            // TODO: 7.   Use Sqlbulk copy datatable.

        }
        
        /// <summary>
        /// 檢查來源及目的資料庫版本
        /// </summary>
        /// <returns>來源大於目的：1、來源等於目的：0、來源小於目的：-1</returns>
        public int CheckVersion()
        {
            int sourceVer = (int)(DbHelper.ReadOne(Source.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));
            int destinationVer = (int)(DbHelper.ReadOne(Destination.ConnectionString(),
                             "Select TOP 1 VersionNum From WebDBVersion Order By klKey DESC"));

            if (sourceVer - destinationVer > 0)
            {
                return 1;
            }
            else if (sourceVer == destinationVer)
            {
                return 0;
            }
            else
            {
                return -1;
            }
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
