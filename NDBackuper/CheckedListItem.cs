using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDBackuper
{
    public class CheckedListItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private bool _ischecked;
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                RaisePropertyChanged("Id");
            }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged("Name"); }
        }
        public bool IsChecked
        {
            get { return _ischecked; }
            set { _ischecked = value; RaisePropertyChanged("IsChecked"); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged(String propertyName)
        {
            if ((PropertyChanged != null))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public struct ConnectionStatus
    {
        public bool IsConnected;
        public string Whois;
    }
}
