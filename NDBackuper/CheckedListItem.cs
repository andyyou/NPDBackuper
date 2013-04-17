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
        private bool _isenable;
        private bool _isoverride;
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
        public bool IsEnable
        {
            get { return _isenable; }
            set { _isenable = value; RaisePropertyChanged("IsEnable"); }
        }
        public bool IsOverride
        {
            get { return _isoverride; }
            set { _isoverride = value; RaisePropertyChanged("IsOverride"); }
        }
        public CheckedListItem()
        {
            this.IsEnable = true;
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
}
