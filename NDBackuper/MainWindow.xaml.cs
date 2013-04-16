using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Notify Property
        
        private bool source_login_security;
        private bool destination_login_security;
        public bool UseSourceLoginSecurity
        {
            get
            {
                return source_login_security;
            }
            set
            {
                source_login_security = value;
                RaisePropertyChanged("UseSourceLoginSecurity");
            }
        }
        public bool UseDestinationLoginSecurity {
            get
            {
                return destination_login_security;
            }
            set
            {
                destination_login_security = value;
                RaisePropertyChanged("UseDestinationLoginSecurity");
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
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

        }
        #endregion
        
        #region Wizard Events
        // Close
        private void wzdMain_Cancelled(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Wizard_Commit(object sender, AvalonWizard.WizardPageConfirmEventArgs e)
        {

        }

        #endregion
    }
}
