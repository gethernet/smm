using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace smm.GUI
{
    /// <summary>
    /// Interaction logic for CopyWindow.xaml
    /// </summary>
    public partial class CopyWindow : Window, INotifyPropertyChanged
    {
        #region Variables / Properties
                public event PropertyChangedEventHandler PropertyChanged;

                private string m_sModuleName;
                public string p_sModuleName { get { return m_sModuleName; } set { m_sModuleName = value; } }

                private string m_sVersionName;
                public string p_sVersionName { get { return m_sVersionName; } set { m_sVersionName = value; } }

                private string m_sCopyName;
                public string p_sCopyName { get { return m_sCopyName; } set { m_sCopyName = value; NotifyPropertyChanged (); } }

                private bool m_bApplyRecursive;
                public bool p_bApplyRecursive
                {
                    get { return m_bApplyRecursive; }
                    set
                    {
                        m_bApplyRecursive = value;
                        m_ctrlCopyNameLabel.Visibility = (m_bApplyRecursive ? Visibility.Visible : Visibility.Hidden);
                        m_ctrlCopyName.Visibility = (m_bApplyRecursive ? Visibility.Visible : Visibility.Hidden);
                    }
                }
        #endregion

        #region Constructors
                public CopyWindow ()
                {
                    m_bApplyRecursive = true;
                }

                public bool ShowModal ()
                {
                    InitializeComponent ();
                    bool? b = ShowDialog ();

                    return (true == b);
                }
        #endregion

        #region Event handlers
                // This method is called by the Set accessor of each property.
                // The CallerMemberName attribute that is applied to the optional propertyName
                // parameter causes the property name of the caller to be substituted as an argument.
                private void NotifyPropertyChanged ([CallerMemberName] String propertyName = "")
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
                    }
                }

                private void OK_Click (object sender, RoutedEventArgs e)
                {
                    DialogResult = true;
                    Close ();
                }

                private void Cancel_Click (object sender, RoutedEventArgs e)
                {
                    DialogResult = false;
                    Close ();
                }

                private void ModuleName_TextChanged(object sender, TextChangedEventArgs e)
                {
                    p_sCopyName = ((TextBox)sender).Text + "-" + p_sVersionName;
                }

                private void VersionName_TextChanged(object sender, TextChangedEventArgs e)
                {
                    p_sCopyName = p_sModuleName + "-" + ((TextBox)sender).Text;
                }
        #endregion
    }
}
