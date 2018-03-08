using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using smm.Subversion;
using smm.GUI;

namespace smm.GUI
{
    #region Types
            public enum CopyType
            {
                Tag = 0x00,
                Branch = 0x01
            }
    #endregion

    public class BranchTagTree
    {
        #region Variables
                IDataElement    m_iRootElement;
                CopyType        m_eType;
                string          m_sRootProject;
                string          m_sURLVersionPath;

                //Task            m_tsk;
                bool            m_bResult;
                bool            m_bIgnoreExistingTargets;
        #endregion

        #region Constructors
                public BranchTagTree (IDataElement iRootElement, CopyType eType, string sRootProject, string sURLVersionPath)
                {
                    m_iRootElement = iRootElement;
                    m_eType = eType;
                    m_sRootProject = sRootProject;
                    m_sURLVersionPath = sURLVersionPath;

                    m_bIgnoreExistingTargets = false;
                }
        #endregion

        #region Functions
                public bool CreateSubCopies ()
                {
                    MainWindow wnd = (MainWindow)m_iRootElement.MainWnd;
                    wnd.EnableControls (false);

                        m_bResult = CreateSubCopies (m_iRootElement, "");

                    wnd.EnableControls (true);
                    return m_bResult;
                }

                private bool CreateSubCopies (IDataElement iDataElement, string sRelativePath)
                {
                    // traverse through childs first
                    foreach (IDataElement itm in iDataElement.DataChilds)
                    {
                        string sPath = sRelativePath;
                        if (itm.Name.Length > 0)
                            sPath = sPath + "/" + itm.Name;

                        if (!CreateSubCopies (itm, sPath))
                            return false;
                    }

                    // if working copy, create a copy for the links above
                    if ((iDataElement is WorkingCopy) &&
                        (sRelativePath.Length > 0)) ///< only applies if working copy of sub/external (not same as top project)
                    {
                        WorkingCopy wc = (WorkingCopy)iDataElement;

                        if (null == wc.m_xInfo)
                            wc.m_xInfo = SvnXmlInfo.GetInfo (wc.m_sPath);

                        string sURLVersionPath2,
                                sURLVersionBase = General.GetVersionBase (wc.m_xInfo.m_Entries[0].m_sURL, out sURLVersionPath2),
                                sURL = sURLVersionBase + m_sURLVersionPath;

                        if (sURLVersionBase.Length > 0)
                        {
                            // check, if target path exists
                            SvnXmlInfo xTarget = SvnXmlInfo.GetInfo (sURL);
                            if ((xTarget == null) || (xTarget.m_Entries.Count == 0))
                            {
                                int iRet = svn.Copy (wc.m_sPath, sURL, "hierarchal copy " + m_sRootProject + sRelativePath + " as " + m_sURLVersionPath, true);
                                if (iRet != 0)
                                    return false;
                            }
                            else
                            {
                                if (!m_bIgnoreExistingTargets)
                                {
                                    MainWindow wnd = (MainWindow)iDataElement.MainWnd;
                                    switch (MessageBox.Show ("Target URL\n" + sURL + "\nalready exists.\n\nContinue?\n\n(cancel = do not ask again)", "Target exists", MessageBoxButton.YesNoCancel))
                                    {
                                        case MessageBoxResult.No:
                                            return false;

                                        case MessageBoxResult.Cancel:
                                            m_bIgnoreExistingTargets = true;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    // re-map version elements to project-specific copy
                    if (iDataElement is External)
                    {
                        External ext = (External)iDataElement;
                        if (ext.m_sURLVersionBase.Length > 0)
                        {
                            ext.m_sURLVersionPathTemp = ext.m_sURLVersionPath;
                            ext.m_sURLVersionPath = m_sURLVersionPath;

                            ext.m_sPegRevisionTemp = ext.m_sPegRevision;
                            ext.m_sPegRevision = "";

                            ext.m_sOpRevisionTemp = ext.m_sOpRevision;
                            ext.m_sOpRevision = "";

                            // update view
                            if (iDataElement.TreeItem != null)
                            {
                                MainWindow wnd = (MainWindow)m_iRootElement.MainWnd;
                                TreeItem ti = (TreeItem)iDataElement.TreeItem;
                                ti.VersionPath = ti.VersionPath;
                                ti.Revision = ti.Revision;
                                wnd.RefreshTreeView ();
                            }
                        }
                    }

                    // apply changes of externals
                    if (iDataElement is FolderWithExternals)
                    {
                        FolderWithExternals fld = (FolderWithExternals)iDataElement;
                        fld.ApplyChanges (false, true, true);
                    }

                    return true;
                }

                public bool RevertSubCopies ()
                {
                    MainWindow wnd = (MainWindow)m_iRootElement.MainWnd;
                    wnd.EnableControls (false);

                        m_bResult = RevertSubCopies (m_iRootElement);

                    wnd.EnableControls (true);
                    return m_bResult;
                }

                /// <summary>
                /// Reverts recursively the changes, made before a tree tag/branch
                /// </summary>
                /// <returns>true, if successful</returns>
                private bool RevertSubCopies (IDataElement iDataElement)
                {
                    // traverse through childs first
                    foreach (IDataElement itm in iDataElement.DataChilds)
                    {
                        if (!RevertSubCopies (itm))
                            return false;
                    }

                    // reset temporary version mappings
                    if (iDataElement is External)
                    {
                        External ext = (External)iDataElement;
                        if (ext.m_sURLVersionBase.Length > 0)
                        {
                            ext.m_sURLVersionPath = ext.m_sURLVersionPathTemp;
                            ext.m_sPegRevision = ext.m_sPegRevisionTemp;
                            ext.m_sOpRevision = ext.m_sOpRevisionTemp;

                            // update view
                            if (iDataElement.TreeItem != null)
                            {
                                MainWindow wnd = (MainWindow)m_iRootElement.MainWnd;
                                TreeItem ti = (TreeItem)iDataElement.TreeItem;
                                ti.VersionPath = ti.VersionPath;
                                ti.Revision = ti.Revision;
                                wnd.RefreshTreeView ();
                            }
                        }
                    }

                    // apply external changes
                    if (iDataElement is FolderWithExternals)
                    {
                        FolderWithExternals fld = (FolderWithExternals)iDataElement;
                        fld.ApplyChanges (false, true, true);
                    }

                    return true;
                }
        #endregion
    }
    
    public class TreeItem : IViewElement, INotifyPropertyChanged
    {
        #region Types
                public enum ElementType
                {
                    Unknown             = 0x00,
                    WorkingCopy         = 0x01,
                    FolderWithExternals = 0x02,
                    External            = 0x04
                };
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        # region Variables
                public event PropertyChangedEventHandler PropertyChanged;

                public bool m_bIsSelected;
                public IDataElement m_DataElement;
                public ObservableCollection<TreeItem> m_Childs;
                public ElementType m_eType;
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
                public string Name
                {
                    get { return m_DataElement.Name; }
                    //set { NotifyPropertyChanged (); }
                }

                public string Path
                {
                    get { return m_DataElement.Path; }
                    //set { NotifyPropertyChanged (); }
                }

                public string VersionPath
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External: return ((External)m_DataElement).VersionPath;
                        }

                        return "";
                    }
                    set { NotifyPropertyChanged (); FontWeight = ""; Foreground = ""; }
                }

                public string VersionBase
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External: return ((External)m_DataElement).VersionBase;
                        }

                        return "";
                    }
                    set { NotifyPropertyChanged (); }
                }

                public string Revision
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External: return ((External)m_DataElement).Revision;
                        }

                        return "";
                    }
                    set { NotifyPropertyChanged (); FontWeight = ""; Foreground = ""; }
                }

                public string FontWeight
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External:  return ((External)m_DataElement).FontWeight;
                        }

                        return "";
                    }
                    set { NotifyPropertyChanged (); }
                }

                public string Foreground
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External: return ((External)m_DataElement).Foreground;
                        }

                        return "";
                    }
                    set { NotifyPropertyChanged (); }
                }

                public string ToolTipNames
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External:              return ((External)m_DataElement).ToolTipNames;
                            case ElementType.FolderWithExternals:   return "Path:";
                            case ElementType.WorkingCopy:           return "Path:";
                        }

                        return "";
                    }
                }

                public string ToolTipData
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.External:              return ((External)m_DataElement).ToolTipData;
                            case ElementType.FolderWithExternals:   return ((FolderWithExternals)m_DataElement).Path;
                            case ElementType.WorkingCopy:           return ((WorkingCopy)m_DataElement).Path;
                        }

                        return "";
                    }
                }

                public IEnumerable<IViewElement> ViewChilds
                {
                    get { return m_Childs; }
                }

                public bool IsSelected
                {
                    get { return m_bIsSelected; }
                    set { m_bIsSelected = value; }
                }

                private static string m_sIconWorkingCopy = "/icons/WorkingCopy.png";
                private static string m_sIconExternal = "/icons/External.png";
                //private static string m_sIconFolderWithExternals = "/icons/FolderWithExternals.png";

                public string Icon
                {
                    get
                    {
                        switch (m_eType)
                        {
                            case ElementType.WorkingCopy:           return m_sIconWorkingCopy;
                            case ElementType.FolderWithExternals:   return m_sIconWorkingCopy;
                            case ElementType.External:              return m_sIconExternal;
                        }

                        return "";
                    }
                }

                private ICommand _SwitchVersion;
                public ICommand SwitchVersion
                {
                    get
                    {
                        if (null == _SwitchVersion)
                        {
                            _SwitchVersion = new RelayCommand (
                                () => this.OnMouseDoubleClick (null, null),
                                () => (//m_eType.HasFlag (ElementType.WorkingCopy) ||
                                        m_eType.HasFlag (ElementType.External)));
                        }
                        return _SwitchVersion;
                    }
                }

                private ICommand _CreateBranch;
                public ICommand CreateBranch
                {
                    get
                    {
                        if (null == _CreateBranch)
                        {
                            _CreateBranch = new RelayCommand
                            (
                                () => this.OnCreateBranch (),
                                () =>   (
                                            (
                                                m_eType.HasFlag (ElementType.WorkingCopy) ||
                                                m_eType.HasFlag (ElementType.External)
                                            )
                                            &&
                                            (
                                                (m_eType != ElementType.External) ||
                                                (((External)m_DataElement).m_sURLVersionPath.Length > 0)
                                            )
                                        )
                            );
                        }
                        return _CreateBranch;
                    }
                }

                private ICommand _CreateTag;
                public ICommand CreateTag
                {
                    get
                    {
                        if (null == _CreateTag)
                        {
                            _CreateTag = new RelayCommand
                            (
                                () => this.OnCreateTag (),
                                () => (
                                            (
                                                m_eType.HasFlag (ElementType.WorkingCopy) ||
                                                m_eType.HasFlag (ElementType.External)
                                            )
                                            &&
                                            (
                                                (m_eType != ElementType.External) ||
                                                (((External)m_DataElement).m_sURLVersionPath.Length > 0)
                                            )
                                        )
                            );
                        }
                        return _CreateTag;
                    }
                }

                private ICommand _Browse;
                public ICommand Browse
                {
                    get
                    {
                        if (null == _Browse)
                        {
                            _Browse = new RelayCommand
                            (
                                () => this.OnBrowse (),
                                () => true
                            );
                        }
                        return _Browse;
                    }
                }

        /*
                    private ICommand _AddExternal;
                    public ICommand AddExternal
                    {
                        get
                        {
                            if (null == _AddExternal)
                            {
                                _AddExternal = new RelayCommand (
                                    () => this.OnAddExternal (),
                                    () =>   !m_DataElement.IsChanged && (
                                                m_eType.HasFlag (ElementType.FolderWithExternals) ||
                                                m_eType.HasFlag (ElementType.External) ||
                                                m_eType.HasFlag (ElementType.WorkingCopy)
                                            ));
                            }
                            return _AddExternal;
                        }
                    }

                    private ICommand _RemoveExternal;
                    public ICommand RemoveExternal
                    {
                        get
                        {
                            if (null == _RemoveExternal)
                            {
                                _RemoveExternal = new RelayCommand (
                                    () => this.OnRemoveExternal (),
                                    () => (m_eType == ElementType.External));
                            }
                            return _RemoveExternal;
                        }
                    }
            */
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Constructors
                public TreeItem (IDataElement DataElement, bool bRecursive = true)
                {
                    m_DataElement = DataElement;
                    LoadElement (bRecursive);
                }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Functions
                public void LoadElement (bool bRecursive = true)
                {
                    m_DataElement.TreeItem = this;
                    if (null == m_Childs)
                        m_Childs = new ObservableCollection<TreeItem> ();

                    // trigger view update
                    VersionPath = "";
                    Revision = "";

                    m_eType = ElementType.Unknown;
                    if (m_DataElement.GetType() == typeof(WorkingCopy)) m_eType = ElementType.WorkingCopy;
                    if (m_DataElement.GetType() == typeof(FolderWithExternals)) m_eType = ElementType.FolderWithExternals;
                    if (m_DataElement.GetType() == typeof(External)) m_eType = ElementType.External;

                    if (bRecursive)
                    {
                        foreach (IDataElement itm in m_DataElement.DataChilds)
                        {
                            IDataElement sel = itm;
                            while (("" == sel.Name) && (sel.DataChilds.Count () > 0))
                                sel = sel.DataChilds.ElementAt (0);

                            if ("" != sel.Name)
                                m_Childs.Add (new TreeItem (sel));
                        }
                    }
                }

                public static TreeItem NextTreeItem (TreeItem tiParent, IDataElement iCurrent)
                {
                    if (iCurrent.Name == "")
                    {
                        return tiParent;
                    }
                    else
                    {
                        TreeItem ti = new TreeItem (iCurrent);
                        tiParent.m_Childs.Add (ti);

                        // http://www.jonathanantoine.com/2011/08/29/update-my-ui-now-how-to-wait-for-the-rendering-to-finish/
                        MainWindow wnd = (MainWindow)iCurrent.MainWnd;
                        wnd.m_ctrlTreeView.Dispatcher.Invoke (new Action (() => { }), DispatcherPriority.ContextIdle, null);

                        return ti;
                    }
                }
        #endregion

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

                public bool OnMouseDoubleClick (TreeView tv, TreeViewItem item)
                {
                    return m_DataElement.OnMouseDoubleClick(tv, item);
                }
                
                public void OnSwitchVersion ()
                {
                    OnMouseDoubleClick (null, null);
                }

                private void OnCreateBranch ()
                {
                    CreateCopy (CopyType.Branch);
                }

                private void OnCreateTag ()
                {
                    CreateCopy (CopyType.Tag);
                }

                private void CreateCopy (CopyType eType)
                {
                    // determine module name and URL
                    string      sURLVersionBase = "",
                                sModuleName = Name,
                                sPath = "";

                    switch (m_eType)
                    {
                        case ElementType.WorkingCopy:
                                sPath = ((WorkingCopy)m_DataElement).m_sPath;
                                break;
                        case ElementType.External:
                                sPath = ((External)m_DataElement).m_wc[0].m_sPath;
                                break;
                    }
                    SvnXmlInfo  info = SvnXmlInfo.GetInfo (sPath);
                    string      sURLVersionPath;
                    sURLVersionBase = General.GetVersionBase (info.m_Entries[0].m_sURL, out sURLVersionPath);
                    sModuleName = General.GetLastPart (sURLVersionBase.Remove (sURLVersionBase.Length-1), '/');

                    // show input box for parameters
                    CopyWindow cw = new CopyWindow ();
                    cw.p_sModuleName = sModuleName;
                    cw.p_sVersionName = "V1.0.0";
                    if (cw.ShowModal ())
                    {
                        // determine version path
                        string sTypePrefix = "";
                        switch (eType)
                        {
                            case CopyType.Branch:
                                    sTypePrefix = "branches/";
                                    break;

                            case CopyType.Tag:
                                    sTypePrefix = "tags/";
                                    break;
                        }
                        string sSubName = cw.p_sCopyName;

                        // check if non-empty and a valid file name
                        if ((sSubName.Length > 0) &&
                            (-1 == sSubName.IndexOfAny (System.IO.Path.GetInvalidPathChars ()))) // https://stackoverflow.com/questions/3067479/determine-via-c-sharp-whether-a-string-is-a-valid-file-path
                        {
                            string sSubPath = sTypePrefix + sSubName;
                            BranchTagTree btTree = new BranchTagTree (m_DataElement, eType, sModuleName, sSubPath);

                            // apply recursive first, if checked
                            bool bRecursiveResult = true;
                            if (cw.p_bApplyRecursive)
                                bRecursiveResult = btTree.CreateSubCopies ();

                            // commit top level with changes applied
                            if (bRecursiveResult)
                            {
                                TortoiseProc.Copy (m_DataElement.Path, sURLVersionBase + sTypePrefix + cw.p_sVersionName, "hierarchal copy " + sSubPath, true, false, TortoiseProc.ExitType.CloseOnNoErrorsAndConflictsAndMerges);

                                // revert temporary changed externals
                                if (cw.p_bApplyRecursive)
                                    btTree.RevertSubCopies ();
                            }
                        }
                    }
                }

                private void OnBrowse ()
                {
                    string  sURL = m_DataElement.Path,
                            sTarget;
                    int     iRevision = -1;

                    // determine specified revision, if any
                    if (m_eType == ElementType.External)
                    {
                        External ext = (External)m_DataElement;
                        if (ext.m_sPegRevision.Length > 0)
                            iRevision = Convert.ToInt32 (ext.m_sPegRevision);
                        if (ext.m_sOpRevision.Length > 0)
                            iRevision = Convert.ToInt32 (ext.m_sOpRevision);
                    }
                    Task.Run (() => { TortoiseProc.BrowseRepos (sURL, out sTarget, iRevision);});
                    
                }

                private void OnAddExternal ()
                {
                    string  sStartPath,
                            sURL;

                    // determine URL of current selected element as starting point for browsing
                    sStartPath = m_DataElement.Path;

                    string  sTempFile,
                            sStdOut,
                            sStdErr;

                    MainWindow wndMain = (MainWindow)m_DataElement.MainWnd;
                    //wndMain.RefreshTreeView ();

                    // get local path
                    string sLocalPath = Interaction.InputBox ("Please type the local path", "Add external");
                    if (sLocalPath != "")
                    {
                        // call TortoiseSVN to select the target path
                        sTempFile = General.GetTempFileName ();
                        if (0 == General.ShellWait ("TortoiseProc.exe", "/command:repobrowser /path:\"" + sStartPath + "\" /outfile:\"" + sTempFile + "\"", out sStdOut, out sStdErr))
                        {
                            // show any error message
                            if (sStdErr.Length > 0)
                            {
                                MessageBox.Show (sStdErr, "Error");
                            } else
                            {
                                // output file exists?
                                if (!File.Exists (sTempFile))
                                {
                                    MessageBox.Show (sTempFile, "Temporary file not found!");
                                }
                                else
                                {
                                    string[]    asOutput,
                                                asSeparatorNewLine = { "\n" };
                                    string      sOutput;

                                    // read output file
                                    sOutput = File.ReadAllText (sTempFile);
                                    File.Delete (sTempFile); // remove parameter file

                                    // path selected?
                                    if (sOutput.Length > 0)
                                    {
                                        // split lines
                                        asOutput = sOutput.Split (asSeparatorNewLine, StringSplitOptions.None);
                                        if (asOutput.Count () > 1)
                                        {
                                            string  sTargetURL = asOutput[0],
                                                    sRevision = asOutput[1];

                                            SvnXmlInfo info = SvnXmlInfo.GetInfo (sStartPath);
                                            Uri uStart = new Uri (info.m_Entries[0].m_sURL);
                                        
                                            sURL = uStart.MakeRelativeUri (new Uri (sTargetURL)).ToString ();
                                            string sURLAndRevision = Interaction.InputBox ("Edit relative path","Add external",sURL + "@" + sRevision);
                                            if (sURLAndRevision != "")
                                            {
                                                //MessageBox.Show (sURLAndRevision, "Add external");
                                                string[] asSeparatorAt = { "@" };
                                                string[] asURLAndRevision = sURLAndRevision.Split (asSeparatorAt, StringSplitOptions.None);

                                                // extract parts
                                                sURL = asURLAndRevision[0];
                                                if (asURLAndRevision.Count () > 1)
                                                    sRevision = asURLAndRevision[1];
                                                if (sRevision == "HEAD")
                                                    sRevision = "";

                                                // create intermediate elements until FolderWithExternal parent
                                                FolderWithExternals fldParent = null;

                                                switch (m_eType)
                                                {
                                                    case ElementType.WorkingCopy:
                                                        {
                                                            WorkingCopy wc = (WorkingCopy)m_DataElement;
                                                            fldParent = new FolderWithExternals (".", wc, wndMain);
                                                            wc.m_aFolders.Add (fldParent);
                                                        }
                                                        break;

                                                    case ElementType.FolderWithExternals:
                                                        {
                                                            fldParent = (FolderWithExternals)m_DataElement;
                                                        }
                                                        break;

                                                    case ElementType.External:
                                                        {
                                                            External ext = (External)m_DataElement;
                                                            WorkingCopy wc = new WorkingCopy (".", ext, wndMain);
                                                            fldParent = new FolderWithExternals (".", wc, wndMain);
                                                            wc.m_aFolders.Add (fldParent);
                                                        }
                                                        break;
                                                }

                                                // create external
                                                string sRevisionExt = "";
                                                if (sRevision != "")
                                                    sRevisionExt = "@" + sRevision;
                                                External ext2 = new External (m_DataElement.MainWnd,
                                                                             sURL + sRevisionExt + " " + sLocalPath,
                                                                             fldParent,
                                                                             this);
                                                ext2.m_sPath = sLocalPath;
                                                ext2.m_sURL = sURL;
                                                ext2.m_sOpRevision = sRevision;

                                                // insert external
                                                fldParent.m_aExternals.Add (ext2);

                                                // create and add new tree element
                                                TreeItem tiExt = new TreeItem (ext2);
                                                m_Childs.Add (tiExt);

                                                // refresh view
                                                MainWindow wnd = (MainWindow)ext2.m_wndMain;
                                                //wnd.RefreshTreeView ();
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show ("Unknown return format of TortoiseSVN call.", "Error");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                private void OnRemoveExternal ()
                {
                    External ext = (External) m_DataElement;
                    FolderWithExternals fld = ext.m_fldParent;

                    // remove data element
                    int i = fld.m_aExternals.IndexOf (ext);
                    fld.m_aExternals.RemoveAt (i);

                    TreeItem    tiExternal = (TreeItem)ext.m_objTreeItem,
                                tiParent = (TreeItem)fld.m_objTreeItem;

                    // remove tree view element
                    i = tiParent.m_Childs.IndexOf (tiExternal);
                    tiParent.m_Childs.RemoveAt (i);

                    MainWindow wnd = (MainWindow)ext.m_wndMain;
                    //wnd.RefreshTreeView ();
                }
        #endregion
    }
}
