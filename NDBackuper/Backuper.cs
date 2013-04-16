using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDBackuper
{
    public class Backuper
    {
        private int progress = 0;
        protected List<string> TablesOfBackup { get; set; }
        public ConnectionConfig Source { get; set; }
        public ConnectionConfig Destination { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsDateFiltration { get; set; }

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
        
        }

        public int GetProgress()
        {
            return progress;
        }

        protected bool CheckVersion()
        {
            return false;
        }

    }
}
