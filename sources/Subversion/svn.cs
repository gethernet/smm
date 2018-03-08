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
    static public class svn
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Types
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Functions
                static public int Update (string sPath)
                {
                    string  sStdOut,
                            sStdErr;

                    return General.ShellWait ("svn.exe", "update \"" + sPath + "\"", out sStdOut, out sStdErr);
                }

                static public int Copy (string sPath, string sURL, string sLogMsg = "", bool bMakeParents = false)
                {
                    string  sStdOut,
                            sStdErr,
                            sLogParam = "",
                            sMakeParentsParam = "";

                    if (sLogMsg.Length > 0)
                    {
                        sLogParam = " -m \"" + sLogMsg + "\"";
                    }

                    if (bMakeParents)
                        sMakeParentsParam = " --parents";

                    return General.ShellWait ("svn.exe", "copy \"" + sPath + "\" \"" + sURL + "\"" + sLogParam + sMakeParentsParam, out sStdOut, out sStdErr);
                }

        #endregion

    }
}
