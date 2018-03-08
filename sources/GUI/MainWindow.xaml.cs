using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using smm.Subversion;
using smm.GUI;

namespace smm
{
    public partial class MainWindow : Window
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                public WorkingCopy m_wcRoot;
                public TreeItem m_ViewContext,
                                m_ViewRoot;
                public DateTime m_tLastUpdate;
        #endregion

        #region Properties
                public bool Updateable
                {
                    get
                    {
                        /*DateTime tNow = DateTime.Now;
                        if (tNow.Subtract (m_tLastUpdate).TotalSeconds > 1)
                        {
                            m_tLastUpdate = tNow;
                            return true;
                        }
                        else
                            return false;*/
                        return true;
                    }

                    set
                    {
                        m_tLastUpdate = DateTime.Now;
                    }
                }
        
                #region ScaleValue Depdency Property
                        // http://stackoverflow.com/questions/3193339/tips-on-developing-resolution-independent-application/5000120#5000120
                        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register ("ScaleValue", typeof (double), typeof (MainWindow), new UIPropertyMetadata (1.0, new PropertyChangedCallback (OnScaleValueChanged), new CoerceValueCallback (OnCoerceScaleValue)));

                        private static object OnCoerceScaleValue (DependencyObject o, object value)
                        {
                            MainWindow mainWindow = o as MainWindow;
                            if (mainWindow != null)
                                return mainWindow.OnCoerceScaleValue ((double)value);
                            else
                                return value;
                        }

                        private static void OnScaleValueChanged (DependencyObject o, DependencyPropertyChangedEventArgs e)
                        {
                            MainWindow mainWindow = o as MainWindow;
                            if (mainWindow != null)
                                mainWindow.OnScaleValueChanged ((double)e.OldValue, (double)e.NewValue);
                        }

                        protected virtual double OnCoerceScaleValue (double value)
                        {
                            if (double.IsNaN (value))
                                return 1.0f;

                            value = Math.Max (0.1, value);
                            return value;
                        }

                        protected virtual void OnScaleValueChanged (double oldValue, double newValue)
                        {

                        }

                        public double ScaleValue
                        {
                            get
                            {
                                return (double)GetValue (ScaleValueProperty);
                            }
                            set
                            {
                                SetValue (ScaleValueProperty, value);
                            }
                        }
                #endregion

                private void MainGrid_SizeChanged (object sender, EventArgs e)
                {
                    CalculateScale ();
                }

                private void CalculateScale ()
                {
                    double yScale = ActualHeight / 250f;
                    double xScale = ActualWidth / 200f;
                    double value = Math.Min (xScale, yScale);
                    ScaleValue = (double)OnCoerceScaleValue (myMainWindow, value);
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Constructors
                public MainWindow ()
                {
                    m_tLastUpdate = DateTime.Now;

                    InitializeComponent ();

                    this.Show ();
                    LoadContent ();
                    if (null != m_wcRoot)
                        Title = m_wcRoot.Path + " - " + Title;
                }
        #endregion

        #region Functions
                public void EnableControls (bool bEnabled)
                {
                    m_ctrlTreeView.IsEnabled = bEnabled;
                    m_ctrlSelect.IsEnabled = bEnabled;
                    m_ctrlOK.IsEnabled = bEnabled;
                    m_ctrlApply.IsEnabled = bEnabled;
                    m_ctrlReload.IsEnabled = bEnabled;
                    Dispatcher.Invoke (new Action (() => { }), DispatcherPriority.ContextIdle, null);
                }

                private char[] m_acParamRemoveChars = { ' ', '\"', '\\' };

                public void LoadContent ()
                {
                    string[]    args = Environment.GetCommandLineArgs();
                    string      sPath = ".",
                                sStdErr;

                    if (args.Length > 1)
                        sPath = args[1].TrimEnd (m_acParamRemoveChars);

                    // check for URL parameter
                    if (sPath.IndexOf ('/') > 0)
                    {
                        MessageBox.Show ("Only local paths allowed:\n\n" + sPath, "Error");
                        this.Close ();
                    }
                    else
                    {
                        EnableControls (false);
                        m_ctrlTreeView.DataContext = null;

                        m_wcRoot = WorkingCopy.GetExternals (this, out sStdErr, m_ViewRoot, System.IO.Path.GetFullPath (sPath), null);
                        /*if (null != m_wcRoot)
                        {
                            m_ViewRoot = new TreeItem (m_wcRoot);
                            m_ViewContext = new TreeItem (new WorkingCopy ());
                            m_ViewContext.m_Childs.Add (m_ViewRoot);

                            m_ctrlTreeView.DataContext = null;      // required for final update of layout
                            m_ctrlTreeView.DataContext = m_ViewContext;
                        }*/

                        EnableControls (true);
                        m_ctrlTreeView.Focus ();

                        if (null == m_wcRoot)
                        {
                            MessageBox.Show ("No externals or no working copy found:\n\n" + sStdErr, "Error");
                            this.Close ();
                        }
                    }
                }

                public void ReloadContent ()
                {
                    EnableControls (false);
                        m_wcRoot.RereadChanges ();
                    EnableControls (true);
                    m_ctrlTreeView.Focus ();
                }
        #endregion

        #region Event handlers
                private void OnItemMouseDoubleClick (object sender, MouseButtonEventArgs e)
                {
                    if (sender is TreeViewItem)
                    {
                        if (!((TreeViewItem)sender).IsSelected)
                        {
                            return;
                        }
                    }

                    var clickedItem = TryGetClickedItem (e);
                    if (clickedItem == null)
                        return;

                    e.Handled = true; // to cancel expanded/collapsed toggle
                    INotifyDoubleClick ctx = (INotifyDoubleClick) clickedItem.DataContext;
                    Select_Click (sender, e);
                    ctx.OnMouseDoubleClick (m_ctrlTreeView, (TreeViewItem) sender);
                }

                private void TreeViewItem_PreviewMouseRightButtonDown (object sender, MouseButtonEventArgs e)
                {
                    var clickedItem = TryGetClickedItem (e);
                    if (null == clickedItem)
                        return;

                    clickedItem.Focus ();
                    e.Handled = true;
                }

                TreeViewItem TryGetClickedItem (MouseButtonEventArgs e)
                {
                    var hit = e.OriginalSource as DependencyObject;
                    while (hit != null && !(hit is TreeViewItem))
                        hit = VisualTreeHelper.GetParent (hit);

                    return hit as TreeViewItem;
                }

                private void OK_Click (object sender, RoutedEventArgs e)
                {
                    EnableControls (false);
                    if (m_wcRoot.ApplyChanges ())
                        this.Close ();
                }

                private void Apply_Click (object sender, RoutedEventArgs e)
                {
                    EnableControls (false);
                    if (m_wcRoot.ApplyChanges ())
                        ReloadContent ();
                    else
                        EnableControls (true);
                    m_ctrlTreeView.Focus ();
                }

                private void Cancel_Click (object sender, RoutedEventArgs e)
                {
                    this.Close ();
                }

                private void Select_Click (object sender, RoutedEventArgs e)
                {
                    //EnableControls (false);
                    object sel = m_ctrlTreeView.SelectedItem;
                    if (sel.GetType () == typeof (External))
                    {
                        External ext = (External) sel;
                        ext.OnMouseDoubleClick (m_ctrlTreeView, null);
                    }
                    //EnableControls (true);
                    m_ctrlTreeView.Focus ();
                }

                private void Reload_Click (object sender, RoutedEventArgs e)
                {
                    m_ViewRoot = null;
                    LoadContent ();
                }

                public void RefreshTreeView (bool bSetFocus = true)
                {
                    // replaced by using ObservableCollection
                    // http://stackoverflow.com/questions/11986840/wpf-treeview-refreshing
                    //m_ctrlTreeView.Items.Refresh ();
                    //m_ctrlTreeView.UpdateLayout ();

                    // idle command for enabling GUI updates
                    m_ctrlTreeView.Dispatcher.Invoke (new Action (() => { }), DispatcherPriority.ContextIdle, null);

                    if (bSetFocus)
                        m_ctrlTreeView.Focus ();
                }
        #endregion
    }
}
