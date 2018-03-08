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
    // static class for calling TortoiseSVN's GUI
    // see https://tortoisesvn.net/docs/nightly/TortoiseSVN_en/tsvn-automation.html
    static public class TortoiseProc
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        # region Types
            public enum ExitType
            {
                NoAutoClose                             = 0x00,
                CloseOnNoErrors                         = 0x01,
                CloseOnNoErrorsAndConflicts             = 0x02,
                CloseOnNoErrorsAndConflictsAndMerges    = 0x03,
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Functions
                static public bool BrowseRepos (string sStartPath, out string sOutput, int iRevision = -1)
                {
                    string  sTempFile,
                            sStdOut,
                            sStdErr,
                            sRevOpt = "";

                    sOutput = "";

                    if (iRevision >= 0)
                        sRevOpt = " /rev:" + iRevision.ToString ();

                    // call TortoiseSVN to select the target path
                    sTempFile = General.GetTempFileName ();
                    if (0 == General.ShellWait ("TortoiseProc.exe", "/command:repobrowser /path:\"" + sStartPath + "\" /outfile:\"" + sTempFile + "\"" + sRevOpt, out sStdOut, out sStdErr))
                    {
                        // show any error message
                        if (sStdErr.Length > 0)
                        {
                            MessageBox.Show (sStdErr, "Error");
                        }
                        else
                        {
                            // output file exists?
                            if (!File.Exists (sTempFile))
                            {
                                MessageBox.Show (sTempFile, "Temporary file not found!");
                                return false;
                            }
                            else
                            {
                                // read output file
                                sOutput = File.ReadAllText (sTempFile);
                                File.Delete (sTempFile); // remove parameter file
                            }
                        }
                    }

                    return true;
                }

                static public bool Update (string sPath, ExitType eExitType = ExitType.NoAutoClose)
                {
                    string  sStdOut,
                            sStdErr;

                    if (0 == General.ShellWait ("TortoiseProc.exe", "/command:update /path:\"" + sPath + "\" /closeonend:" + Convert.ToInt16 (eExitType), out sStdOut, out sStdErr))
                        return true;
                    else
                        return false;
                }

                static public bool Copy (string sPath, string sURL, string sLogMsg = "", bool bMakeParents = false, bool bSwitchAfterCopy = false, ExitType eExitType = ExitType.NoAutoClose)
                {
                    string  sStdOut,
                            sStdErr,
                            sLogParam = "",
                            sMakeParentsParam = "",
                            sSwitchAfterCopyParam = "";

                    if (sLogMsg.Length > 0)
                    {
                        sLogParam = " /logmsg:\"" + sLogMsg + "\"";
                    }

                    if (bMakeParents)
                        sMakeParentsParam = " /makeparents";

                    if (bSwitchAfterCopy)
                        sSwitchAfterCopyParam = " /switchaftercopy";

                    if (0 == General.ShellWait ("TortoiseProc.exe", "/command:copy /path:\"" + sPath + "\" /url:\"" + sURL + "\"" + sLogParam + sMakeParentsParam + sSwitchAfterCopyParam + " /closeonend:" + Convert.ToInt16 (eExitType), out sStdOut, out sStdErr))
                        return true;
                    else
                        return false;
                }
        #endregion

    }
}
