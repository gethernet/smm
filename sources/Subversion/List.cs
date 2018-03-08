using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using smm.GUI;

namespace smm.Subversion
{
    public class SvnXmlListCommit
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                [XmlAttribute ("revision")]
                public string m_sRevision;

                [XmlElement ("author")]
                public string m_sAuthor;

                [XmlElement ("date")]
                public string m_sDate;
        #endregion
    }

    public class SvnXmlListEntry : INotifyDoubleClick, ISelectionValidation
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                public SvnXmlList       m_List;             // list sub-elements
                public SvnXmlListEntry  m_eParent;          // parent list element
                public External         m_extParent;        // parent external
                public bool             m_bValidLocation;   // valid for selection as external
                public string           m_sFontWeight;
                public string           m_sForeground;

                ////////////////////////////////////////////////////////////////
                [XmlAttribute ("kind")]
                public string m_sKind;

                [XmlElement ("name")]
                public string m_sName;

                [XmlElement ("size")]
                public string m_sFileSize;

                [XmlElement ("commit")]
                public SvnXmlListCommit m_Commit;

                public bool m_bIsSelected;
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Properties
                public string Name
                {
                    get { return m_sName; }
                }

                public string VersionPath
                {
                    get
                    {
                        if (null == m_eParent)
                            return m_sName;
                        else
                            return m_eParent.VersionPath + "/" + m_sName;
                    }
                }

                // return the External of the current project
                public External ExternalRoot
                {
                    get
                    {
                        if (null == m_extParent)
                            return m_eParent.ExternalRoot;
                        else
                            return m_extParent;
                    }
                }

                public string Foreground
                {
                    get { return m_sForeground; }
                }

                public string FontWeight
                {
                    get { return m_sFontWeight; }
                }

                public List<SvnXmlListEntry> Entries
                {
                    get
                    {
                        if (null == m_List)
                            return null;
                        else
                            return m_List.m_Paths[0].m_Entries;
                    }
                }

                public bool ValidSelection
                {
                    get { return m_bValidLocation; }
                }
                
                public bool IsSelected
                {
                    get { return m_bIsSelected; }
                    set { m_bIsSelected = value; }
                }
                #endregion

        ////////////////////////////////////////////////////////////////
        #region Functions
                public bool SelectItem (string sVersionPath)
                {
                    if (VersionPath == sVersionPath)
                    {
                        IsSelected = true;
                        return true;
                    }
                    else if (null != Entries)
                    {
                        foreach (SvnXmlListEntry entry in Entries)
                        {
                            if (entry.SelectItem (sVersionPath))
                                return true;
                        }
                    }

                    return false;
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Event handlers
                public bool OnMouseDoubleClick (TreeView tv, TreeViewItem item)
                {
                    if (m_bValidLocation)
                    {
                        ExternalRoot.VersionPath = VersionPath;
                        return true;
                    }

                    return false;
                }

                [OnDeserialized]
                public void OnDeserialized ()
                {
                    m_sForeground = General.VersionPathColor (VersionPath);
                    m_bValidLocation = (m_sForeground != "Black");
                    if (m_bValidLocation)
                        m_sFontWeight = "Bold";
                    else
                        m_sFontWeight = "Normal";
                    TreeItem ti = (TreeItem)ExternalRoot.TreeItem;
                    ti.FontWeight = "";
                    ti.Foreground = "";
                }
        #endregion
    }

    public class SvnXmlListPath : ISelectionValidation
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                [XmlAttribute ("path")]
                public string m_sPath;

                [XmlElement ("entry")]
                public List<SvnXmlListEntry> m_Entries;
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Constructors
                public SvnXmlListPath ()
                {
                    m_Entries = new List<SvnXmlListEntry> ();
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Properties
                public string Name
                {
                    get { return m_sPath; }
                }

                public List<SvnXmlListEntry> Entries
                {
                    get { return m_Entries; }
                }

                public bool ValidSelection
                {
                    get { return false; }
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Functions
                public bool SelectItem (string sVersionPath)
                {
                    if (null != Entries)
                    {
                        foreach (SvnXmlListEntry entry in Entries)
                        {
                            if (entry.SelectItem (sVersionPath))
                                return true;
                        }
                    }
                    return false;
                }
        #endregion
    }

    [XmlRoot("lists")]
    public class SvnXmlList
    {
        ////////////////////////////////////////////////////////////////
        #region Variables
                [XmlElement ("list")]
                public List<SvnXmlListPath> m_Paths { get; set; }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Constructors
                public SvnXmlList ()
                {
                    m_Paths = new List<SvnXmlListPath> ();
                }
        #endregion

        ////////////////////////////////////////////////////////////////
        #region Functions
                public static SvnXmlList GetList (string sPath, SvnXmlListEntry eParent = null, External extParent = null )
                {
                    //MessageBox.Show("GetExternals");
                    string  sStdOut,
                            sStdErr;
                    General.ShellWait ("svn.exe", "list --xml \"" + sPath + "\"", out sStdOut, out sStdErr);

                    if (sStdErr.Length == 0)
                    {
                        // https://msdn.microsoft.com/de-de/library/tz8csy73(v=vs.110).aspx
                        XmlSerializer serializer = new XmlSerializer (typeof (SvnXmlList));
                        StringReader reader = new StringReader (sStdOut);
                        SvnXmlList list = (SvnXmlList)serializer.Deserialize (reader);

                        if (list.m_Paths.Count > 0)
                        {
                            foreach (SvnXmlListEntry entry in list.m_Paths[0].m_Entries)
                            {
                                entry.m_eParent = eParent;
                                entry.m_extParent = extParent;
                                General.ProcessOnDeserialize (entry);

                                if (entry.m_sKind == "dir")
                                {
                                    switch (entry.Name)
                                    {
                                        case "branches":
                                            entry.m_List = SvnXmlList.GetList (sPath + "/" + entry.m_sName, entry);
                                            break;
                                        case "tags":
                                            entry.m_List = SvnXmlList.GetList (sPath + "/" + entry.m_sName, entry);
                                            break;
                                    }
                                }
                            }
                        }

                        return list;
                    }
                    else
                    {
                        return null;
                    }
                }
        #endregion
    }
}
