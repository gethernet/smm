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
using System.Windows.Shapes;
using System.Runtime.Serialization;
using smm;
using smm.Subversion;

namespace smm.GUI
{
    /// <summary>
    /// Interaction logic for SelectionWindow.xaml
    /// </summary>
    public partial class SelectionWindow : Window
    {
        public SelectionWindow (External extParent, SvnXmlList aList)
        {
            InitializeComponent ();

            m_ctrlTreeView.DataContext = new
            {
                Paths = aList.m_Paths
            };

            aList.m_Paths [0].SelectItem (extParent.VersionPath);
            m_ctrlTreeView.Focus ();
        }

        void OnItemMouseDoubleClick (object sender, MouseButtonEventArgs e)
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
            ISelectionValidation sel = (ISelectionValidation) clickedItem.DataContext;
            if (sel.ValidSelection)
            {
                INotifyDoubleClick ctx = (INotifyDoubleClick) clickedItem.DataContext;
                if (ctx.OnMouseDoubleClick (m_ctrlTreeView, (TreeViewItem) sender))
                    this.Close ();
            }
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
            object sel = m_ctrlTreeView.SelectedItem;
            if (sel.GetType () == typeof (SvnXmlListEntry))
            {
                SvnXmlListEntry entry = (SvnXmlListEntry) sel;
                if (entry.OnMouseDoubleClick (m_ctrlTreeView, null) )
                    this.Close ();
            }
        }

        private void Cancel_Click (object sender, RoutedEventArgs e)
        {
            this.Close ();
        }
    }
}
