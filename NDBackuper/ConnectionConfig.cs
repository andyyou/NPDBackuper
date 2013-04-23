using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NDBackuper
{
    public class ConnectionConfig : INotifyPropertyChanged
    {
        private const string IP_PATTEN    = @"([2]([5][0-5]|[0-4][0-9])|[0-1]?[0-9]{1,2})(\.([2]([5][0-5]|[0-4][0-9])|[0-1]?[0-9]{1,2})){3}";
        private const string URL_PATTEN   = @"([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,6}";
        private const string LOCAL_PATTEN = @"([l|L][o|O][c|C][a|A][l|L]|\([l|L][o|O][c|C][a|A][l|L][d|D][b|B]\)\\[\w._\\-]*)";
        private const string PIPE_PATTEN = @"^([A-Za-z]{2,})";
        private const string DB_PATTEN    = @"^([A-Za-z]*)";
        private string _name;
        private string _server;
        private string _userid;
        private string _pwd;
        private string _db;
        private bool _loginSecurity;
        private bool _isremember;
        private bool _isvalidate = false;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged("Name"); }
        }
        public string Server
        {
            get { return _server; }
            set
            {
                Regex rexIp = new Regex(IP_PATTEN);
                Regex rexUrl = new Regex(URL_PATTEN);
                Regex rexLocal = new Regex(LOCAL_PATTEN);
                Regex rexPipe = new Regex(PIPE_PATTEN);
                if (rexIp.IsMatch(value) || rexUrl.IsMatch(value) ||
                    rexLocal.IsMatch(value) || rexPipe.IsMatch(value))
                {
                    _server = value;
                    RaisePropertyChanged("Server");
                }
            }
        }
        public string UserId
        {
            get { return _userid; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _userid = value; 
                    RaisePropertyChanged("UserId");
                }
            }
        }
        public string Password
        {
            get { return _pwd; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _pwd = value;
                    RaisePropertyChanged("Password");
                }
            }
        }
        public bool LoginSecurity
        {
            get { return _loginSecurity; }
            set
            {
                _loginSecurity = value;
                RaisePropertyChanged("LoginSecurity");
            }
        }
        public bool IsRemember
        {
            get {
                return _isremember;
            }
            set
            {
                _isremember = value;
                RaisePropertyChanged("IsRemember");
            }
        }
        public bool IsValidate
        { 
            get{
                return _isvalidate;
            }
            set
            {
                _isvalidate = value;
                RaisePropertyChanged("IsValidate");
            }
        }
        public string Database
        {
            get { return _db; }
            set
            {
                Regex rexDb = new Regex(DB_PATTEN);
                if (rexDb.IsMatch(value))
                {
                    _db = value;
                    RaisePropertyChanged("Database");
                }
            }
        }
        public ServerConnection ServerConnection
        {
            get
            {
                if (this.LoginSecurity)
                {
                    return new ServerConnection(this.Server);
                }
                else
                {
                    return new ServerConnection(this.Server, this.UserId, this.Password);
                }
            }
        }
        public ConnectionConfig()
        {
            this.Server = @"local";
            this.LoginSecurity = true;
        }
        public string ConnectionString()
        {
            string conn = "";
            // Local Connection string
            if (this.LoginSecurity)
            {
                if (String.IsNullOrEmpty(this.Server))
                {
                    this.Server = @"local";
                }

                if (string.IsNullOrEmpty(this.Database))
                {

                    conn = string.Format("Server={0};Integrated Security=True;", this.Server);
                }
                else // When database has value
                {
                    conn = string.Format("Server={0};Integrated Security=True;Database={1};", this.Server, this.Database);
                }
            }
            else // Remote Connection string.
            {
                if (string.IsNullOrEmpty(this.Database))
                {
                    conn = string.Format("Server={0};User ID={1};Password={2};", this.Server, this.UserId, this.Password);
                }
                else
                {
                    conn = string.Format("Server={0};User ID={1};Password={2};Database={3};", this.Server, this.UserId, this.Password, this.Database);
                }
            }

            return conn;
        }
        
        public bool RunValidateConnection()
        {
            if (!String.IsNullOrEmpty(this.ConnectionString()))
            {
                using (SqlConnection connection = new SqlConnection(this.ConnectionString()))
                {
                    try
                    {
                        connection.Open();
                        this.IsValidate = true;
                        return this.IsValidate;
                    }
                    catch (SqlException)
                    {
                        this.IsValidate = false;
                        return this.IsValidate;
                    }
                }
            }
            else
            {
                this.IsValidate = false;
                return this.IsValidate;
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
    }
}
