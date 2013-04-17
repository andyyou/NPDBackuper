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

        public void RunBackup()
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += bgwValidateConnection_DoWorkHandler;
            bgw.RunWorkerCompleted += bgwValidateConnection_RunWorkerCompleted;
            bgw.ProgressChanged += bgwValidateConnection_ProgressChanged;
            bgw.WorkerReportsProgress = true;
        }
        protected bool CheckVersion()
        {

            return false;
        }
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
