using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using smm.GUI;

namespace smm.Subversion
{
    public class FolderWithExternals : IDataElement
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                [XmlAttribute ("path")]
                public string m_sPath;
                [XmlElement("property")]
                public string m_sProperty;

                public List<External> m_aExternals;
                public WorkingCopy m_wcParent;
                public Object m_wndMain;
                public Object m_objTreeItem;
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Properties
                public bool IsChanged
                {
                    get { string s; return IsChangedEx (out s); }
                }

                public string Name
                {
                    get
                    {
                        string sPath = Path.ToLower ();
                        if (null == m_wcParent)
                        {
                            return Path;
                        } else
                        {
                            string sWCPath = m_wcParent.m_sPath.ToLower ();

                            if (sPath.Length == sWCPath.Length)
                                return "";
                            else
                                return Path.Substring (sWCPath.Length + 1, sPath.Length - sWCPath.Length - 1);
                        }
                    }
                }

                public string Path
                {
                    get { return m_sPath.Replace ('/', '\\'); }
                }

                /*public string FontWeight
                {
                    get
                    {
                        string sExternals;
                        if (IsChangedEx (out sExternals))
                            return "Bold";
                        else
                            return "Normal";
                    }
                }*/

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
                    get { return m_wcParent; }
                }

                public IEnumerable<IDataElement> DataChilds
                {
                    get { return m_aExternals; }
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Constructors
                public FolderWithExternals ()
                {
                    m_aExternals = new List<External> ();
                    m_wcParent = null;
                    m_wndMain = null;
                    m_objTreeItem = null;
                }

                public FolderWithExternals (string sPath, WorkingCopy wcParent, Object wndMain)
                {
                    m_aExternals = new List<External> ();
                    m_sPath = sPath;
                    m_wcParent = wcParent;
                    m_wndMain = wndMain;
                    m_objTreeItem = null;
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
                public void ExpandExternals(Object wndMain, WorkingCopy wc, TreeItem tiParent)
                {
                    m_wcParent = wc;
                    m_wndMain = wndMain;
                    TreeItem tiNext = smm.GUI.TreeItem.NextTreeItem (tiParent, this);

                    string[] asSeparators = { "\n" };
                    string[] asLines = m_sProperty.Split(asSeparators, StringSplitOptions.None);
                    
                    foreach (string s in asLines)
                    {
                        if (s.Length > 0)
                            m_aExternals.Add(new External(wndMain, s.Trim(), this, tiNext));
                    }
                }

                public bool IsChangedEx (out string sExternals)
                {
                    sExternals = "";

                    foreach (External ext in m_aExternals)
                    {
                        sExternals = sExternals + ext.PropLine + "\n";
                    }

                    return (sExternals.Length > 0) && (sExternals != m_sProperty);
                }

                public bool ApplyChanges (bool bUpdate = true, bool bIgnoreLocalChanges = false, bool bSkipChangesCheck = false)
                {
                    string sExternals;

                    // externals changed?
                    if (IsChangedEx (out sExternals) || bSkipChangesCheck)
                    {
                        // temporary select entry
                        //m_TreeItem.IsSelected = true;

                        bool bForce = true;
                        string sModifiedElements = "";
                        // check for local changes
                        foreach (External ext in m_aExternals)
                        {
                            if (ext.m_wc.Count > 0)
                            {
                                SvnXmlStatus status = SvnXmlStatus.GetStatus (ext.m_wc[0].Path);
                                sModifiedElements = sModifiedElements + status.GetModifiedElements ();
                            }
                        }

                        // ask, if local changes can be ignored
                        if ((sModifiedElements.Length > 0) &&
                            !bIgnoreLocalChanges)
                        {
                            bForce = (MessageBoxResult.Yes ==
                                        MessageBox.Show (   "The following elements have local modifications.\n" +
                                                            "Do you want to proceed?\n\n" + sModifiedElements,
                                                            "Warning", MessageBoxButton.YesNo));
                        }

                        if (bForce)
                        {
                            // because of collision issues with the operative revision -r in externals with the svn propset option -r
                            // we have to use a parameter file
                            string sFilename = General.GetTempFileName ();
                            File.WriteAllText (sFilename, sExternals);
                            if (!File.Exists (sFilename))
                            {
                                MessageBox.Show (sFilename, "Unable to create temporary file!");
                                return false;
                            }

                            string  sStdOut,
                                    sStdErr;
                            General.ShellWait ("svn.exe", "propset svn:externals -F \"" + sFilename + "\" \"" + m_sPath + "\"", out sStdOut, out sStdErr);
                            File.Delete (sFilename); // remove parameter file

                            if (sStdErr.Length == 0)
                            {
                                if (bUpdate)
                                {
                                    bool bResult = TortoiseProc.Update (m_sPath, TortoiseProc.ExitType.CloseOnNoErrors);
                                    // trigger view update
                                    if (bResult)
                                    {
                                        foreach (External ext in m_aExternals)
                                        {
                                            TreeItem ti = (TreeItem)ext.TreeItem;
                                            ti.VersionPath = "";
                                            ti.Revision = "";
                                        }
                                    }
                                    return bResult;
                                }
                            } else
                            {
                                MessageBox.Show (sStdErr, "Error setting property");
                                return false;
                            }
                        }
                        return false;
                    } else
                    {
                        // if no externals changed - traverse through externals
                        foreach (External ext in m_aExternals)
                        {
                            if (null != ext.m_wc)
                            {
                                if (!ext.m_wc[0].ApplyChanges (bUpdate))
                                    return false;
                            }
                        }

                        return true;
                    }
                }

                public bool RereadChanges ()
                {
                    string sExternals;

                    // externals changed?
                    if (IsChangedEx (out sExternals))
                    {
                        m_sProperty = sExternals;
                        //m_aExternals.RemoveAll (item => true);
                        //ExpandExternals (m_wcParent);

                        foreach (External ext in m_aExternals)
                        {
                            if (!ext.RereadChanges ())
                                return false;
                        }
                    }
                    else
                    {
                        // if no externals changed - traverse through externals
                        foreach (External ext in m_aExternals)
                        {
                            if (null != ext.m_wc)
                            {
                                if (!ext.m_wc[0].RereadChanges ())
                                    return false;
                            }
                        }
                    }

                    return true;
                }
        #endregion
    }
}
