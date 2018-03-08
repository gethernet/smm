using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using smm.GUI;

namespace smm.Subversion
{
    [XmlRoot("properties")]
    public class WorkingCopy : IDataElement
    {

        ////////////////////////////////////////////////////////////////
        #region Variables
                [XmlElement("target")]
                public List<FolderWithExternals> m_aFolders;
                
                public string m_sPath;
                public External m_extParent;
                public Object m_wndMain;
                public Object m_objTreeItem;
                public SvnXmlInfo m_xInfo;
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Properties
                public bool IsChanged
                {
                    get { return false; }
                }

                public string Name
                {
                    get
                    {
                        if (null != m_extParent)
                        {
                            string  sPath = Path.ToLower (),
                                    sExtPath = m_extParent.Path.ToLower ();

                            if (sPath.Length == sExtPath.Length)
                                return "";
                            else
                                return Path.Substring (sExtPath.Length + 1, sPath.Length - sExtPath.Length - 1);
                        } else
                            return m_sPath;
                    }
                }

                public string Path
                {
                    get { return m_sPath; }
                }

                public Object TreeItem
                {
                    get { return m_objTreeItem; }
                    set { m_objTreeItem = value; }
                }

                public Object MainWnd
                {
                    get { return m_wndMain; }
                }

                public IDataElement Parent
                {
                    get { return m_extParent; }
                }

                public IEnumerable<IDataElement> DataChilds
                {
                    get { return m_aFolders; }
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Constructors
                public WorkingCopy ()
                {
                    m_aFolders = new List<FolderWithExternals> ();
                    m_extParent = null;
                    m_wndMain = null;
                }

                public WorkingCopy (string sPath, External extParent, Object wndMain)
                {
                    m_aFolders = new List<FolderWithExternals> ();
                    m_sPath = sPath;
                    m_extParent = extParent;
                    m_wndMain = wndMain;
                }

                public static WorkingCopy GetExternals (Object wndMain, out string sStdErr, TreeItem tiParent, string sPath = ".", External extParent = null)
                {
                    //MessageBox.Show("GetExternals");
                    string  sStdOut;
                    General.ShellWait ("svn.exe", "propget svn:externals -R --xml \"" + sPath + "\"", out sStdOut, out sStdErr);
                    TreeItem tiNext = null;

                    if (sStdErr.Length == 0)
                    {
                        // https://msdn.microsoft.com/de-de/library/tz8csy73(v=vs.110).aspx
                        XmlSerializer serializer = new XmlSerializer (typeof (WorkingCopy));
                        StringReader reader = new StringReader (sStdOut);
                        WorkingCopy wc = (WorkingCopy)serializer.Deserialize (reader);

                        wc.m_sPath = System.IO.Path.GetFullPath (sPath);
                        wc.m_wndMain = wndMain;
                        wc.m_extParent = extParent;
                        MainWindow wnd = (MainWindow)wndMain;
                        //wnd.BasePath = wc.m_sPath;

                        if (null == wnd.m_wcRoot)
                            wnd.m_wcRoot = wc;

                        if (wnd.Updateable)
                        {
                            if (null == wnd.m_ViewRoot)
                            {
                                wnd.m_ViewContext = new TreeItem (new WorkingCopy ());
                                wnd.m_ViewRoot = new TreeItem (wnd.m_wcRoot, false);
                                wnd.m_ViewContext.m_Childs.Add (wnd.m_ViewRoot);
                                tiNext = wnd.m_ViewRoot;

                                wnd.m_ctrlTreeView.Dispatcher.Invoke (() => { wnd.m_ctrlTreeView.DataContext = wnd.m_ViewContext; });
                            }
                            else
                            {
                                tiNext = smm.GUI.TreeItem.NextTreeItem (tiParent, wc);
                            }
                        }

                        foreach (FolderWithExternals fld in wc.m_aFolders)
                        {
                            fld.ExpandExternals (wndMain, wc, tiNext);
                        }

                        return wc;
                    }
                    else
                    {
                        return null;
                    }
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Event handlers
                public bool OnMouseDoubleClick (TreeView tv, TreeViewItem item)
                {
                    return false;
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Functions
                public bool ApplyChanges (bool bUpdate = true)
                {
                    foreach (FolderWithExternals fld in m_aFolders)
                    {
                        if (!fld.ApplyChanges (bUpdate))
                            return false;
                    }

                    return true;
                }

                public bool RereadChanges ()
                {
                    foreach (FolderWithExternals fld in m_aFolders)
                    {
                        if (!fld.RereadChanges ())
                            return false;
                    }

                    return true;
                }
        #endregion
    }
}
