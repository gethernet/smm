using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using smm.GUI;

namespace smm.Subversion
{
    public class External : IDataElement
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        # region Variables
                public string m_sURL;
                public string m_sURLVersionBase;
                public string m_sURLVersionPath;
                public string m_sURLVersionPathTemp;
                public string m_sURLVersionPathOriginal;
                public string m_sComment;
                public string m_sPath;
                public string m_sOpRevision;
                public string m_sOpRevisionTemp;
                public string m_sPegRevision;
                public string m_sPegRevisionTemp;
                public FolderWithExternals m_fldParent;
                public List<WorkingCopy> m_wc;
                public Object m_wndMain;
                public Object m_objTreeItem;
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
                public bool IsChanged
                {
                    get { return (m_sURLVersionPath != m_sURLVersionPathOriginal); }
                }

                public string Path
                {
                    get { return m_fldParent.m_sPath.Replace ('/', '\\') + "\\" + m_sPath.Replace ('/', '\\'); }
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
                    get { return m_fldParent; }
                }

                public IEnumerable<IDataElement> DataChilds
                {
                    get { return m_wc; }
                }

                public string Name
                {
                    get { return m_sPath.Replace ('/', '\\'); }
                }

                public string URL
                {
                    get { return m_sURL; }
                }

                public string Revision
                {
                    get
                    {
                        if ((m_sPegRevision.Length > 0) || (m_sOpRevision.Length > 0))
                            return m_sPegRevision + " / " + m_sOpRevision;
                        else
                            return "";
                    }
                    set
                    {
                        m_sOpRevision = value;
                        m_sPegRevision = "";
                    }
                }

                public string ToolTipNames
                {
                    get
                    {
                        return "Path:\n" +
                                "URL:\n" +
                                "Peg revision:\n" +
                                "Operative rev.:";
                    }
                }

                public string ToolTipData
                {
                    get
                    { 
                        return  Path + "\n" +
                                URL + "\n" +
                                m_sPegRevision + "\n" +
                                m_sOpRevision;
                    }
                }

                public string VersionBase
                {
                    get { return m_sURLVersionBase; }
                }

                public string VersionPath
                {
                    get { return m_sURLVersionPath; }
                    set
                    {
                        if (value != m_sURLVersionPath)
                        {
                            Revision = "";  // revisions not supported with name-based externals
                            m_sURLVersionPath = value;
                            TreeItem ti = (TreeItem)m_objTreeItem;
                            while (ti.m_Childs.Count > 0)
                            {
                                ti.m_Childs.RemoveAt (0); // sub-externals are invalidated by path changes
                            }
                            // trigger view update
                            ti.VersionPath = "";
                            ti.Revision = "";
                        }
                    }
                }

                public string Foreground
                {
                    get
                    {
                        return General.VersionPathColor (m_sURLVersionPath);
                    }
                }

                public string FontWeight
                {
                    get
                    {
                        if (IsChanged)
                            return "Bold";
                        else
                            return "Normal";
                    }
                }

                public string PropLine
                {
                    get
                    {
                        string sLine = "";

                        if (m_sComment.Length > 0)
                        {
                            sLine = "#" + m_sComment;
                        }
                        else
                        {
                            if (m_sOpRevision.Length > 0)
                                sLine = "-r " + m_sOpRevision + " ";

                            if (m_sURLVersionBase.Length > 0)
                            {
                                sLine = sLine + m_sURLVersionBase + m_sURLVersionPath;
                            }
                            else
                                sLine = sLine + m_sURL;

                            if (m_sPegRevision.Length > 0)
                                sLine = sLine + "@" + m_sPegRevision;

                            if (m_sPath.IndexOf (' ') > 0)
                                sLine = sLine + " '" + m_sPath + "'";
                            else
                                sLine = sLine + " " + m_sPath;
                        }

                        return sLine;
                    }

                    set
                    {
                        m_sComment = "";
                        m_sOpRevision = "";
                        m_sURL = "";
                        m_sURLVersionBase = "";
                        m_sURLVersionPath = "";
                        m_sPegRevision = "";
                        m_sPath = "";

                        if (value.Substring (0, 1) == "#")
                        {
                            m_sComment = value.Substring (1, value.Length - 1);
                        }
                        else
                        {
                            // with operating revision?
                            if (value.Substring (0, 2) == "-r")
                            {
                                int iPos1 = value.IndexOf (" ");

                                value = value.Substring (3, value.Length - 3);
                                iPos1 = value.IndexOf (" ");

                                m_sOpRevision = value.Substring (0, iPos1);
                                value = value.Substring (iPos1 + 1, value.Length - iPos1 - 1);
                            }

                            // separate parts of external line
                            int iPos = value.IndexOf (" "); // implies, that the first part of line is URL encoded (e.g. %20 instead of space character)
                            if (iPos > 0)
                            {
                                m_sURL = value.Substring (0, iPos);

                                // with peg revision? separate!
                                int iPos1 = m_sURL.LastIndexOf ('@');
                                if (iPos1 >= 0)
                                {
                                    m_sPegRevision = m_sURL.Substring (iPos1 + 1, m_sURL.Length - iPos1 - 1);
                                    m_sURL = m_sURL.Substring (0, iPos1);
                                }

                                // if working copy path is enveloped with ' characters, remove them
                                m_sPath = value.Substring (iPos + 1, value.Length - iPos - 1);
                                if (m_sPath [0] == '\'')
                                {
                                    m_sPath = m_sPath.Substring (1, m_sPath.Length - 2);
                                }

                                ////////////////////////////////////////////////////////////////////////////////////////////
                                // check for and separate version element structure
                                m_sURLVersionBase = General.GetVersionBase (m_sURL, out m_sURLVersionPath);
                                // save for changes
                                m_sURLVersionPathOriginal = m_sURLVersionPath;
                            }
                            else
                                throw new Exception ("external format mismatch");
                        }
                    }
                }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Constructors
                public External ()
                {
                    PropLine = "";
                }

                public External (Object wndMain, string sLine, FolderWithExternals fldParent, TreeItem tiParent)
                {
                    m_wc = new List<WorkingCopy> ();
                    m_fldParent = fldParent;
                    m_wndMain = wndMain;
                    m_objTreeItem = null;

                    ////////////////////////////////////////////////////////////////////////////////////////////
                    // separate external line
                    PropLine = sLine;
                    TreeItem tiNext = smm.GUI.TreeItem.NextTreeItem (tiParent, this);
                    ExpandExternal (tiNext);
                }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Event handlers
                public bool OnMouseDoubleClick (TreeView tv, TreeViewItem item)
                {
                    if (m_sURLVersionBase.Length > 0)
                    {
                        SvnXmlInfo info = (SvnXmlInfo) SvnXmlInfo.GetInfo (Path);
                        if (null != info)
                        {
                            string  sURLVersionBase,
                                    sURLVersionPath;

                            sURLVersionBase = General.GetVersionBase (info.m_Entries [0].m_sURL, out sURLVersionPath);
                            SvnXmlList list = SvnXmlList.GetList (sURLVersionBase, null, this);
                            if (null != list)
                            {
                                MainWindow wnd = (MainWindow)m_wndMain;
                                wnd.EnableControls (false);
                                    SelectionWindow sel = new SelectionWindow (this, list);
                                    sel.ShowDialog ();
                                wnd.EnableControls (true);
                            }
                        }
                        return true;
                    }
                    else
                    {
                        MessageBox.Show ("External not aiming at a trunk/branches/tags structure!", "Error");
                        return false;
                    }
                }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Functions
                public bool ExpandExternal (TreeItem tiParent)
                {
                    // if working copy exist, extend tree
                    if (Directory.Exists (Path))
                    {
                        string sStdErr;
                        m_wc.Add (WorkingCopy.GetExternals (m_wndMain, out sStdErr, tiParent, Path, this));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public bool RereadChanges ()
                {
                    if (IsChanged)
                    {
                        m_wc = new List<WorkingCopy> ();
                        PropLine = PropLine;

                        TreeItem ti = (TreeItem)m_objTreeItem;
                        ExpandExternal (ti);

                        // trigger view update
                        ti.VersionPath = "";
                        ti.Revision = "";
                    }

                    return true;
                }
        #endregion

    }
}
