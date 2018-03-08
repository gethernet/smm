using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Diagnostics;

namespace smm
{
    public class General
    {
        #region Functions
                public static string GetLastPart (string sPath, char cSeparator = '\\')
                {
                    int iLastPos = sPath.LastIndexOf (cSeparator);
                    return sPath.Substring (iLastPos + 1, sPath.Length - iLastPos - 1);
                }

                public static string GetFirstPart (string sPath, char cSeparator = '\\')
                {
                    int iPos = sPath.IndexOf (cSeparator);
                    if (iPos >= 0)
                        return sPath.Substring (0, iPos);
                    else
                        return "";
                }

                public static string GetVersionBase (string sURL, out string sVersionPath)
                {
                    string  sLastPart = General.GetLastPart (sURL, '/'),
                            sButLastPart = sURL.Substring (0, sURL.Length - sLastPart.Length);

                    // is linked with trunk
                    if (sLastPart == "trunk")
                    {
                        sVersionPath = sLastPart;
                        return sButLastPart;
                    }
                    else if (sButLastPart != "")
                    {
                        sButLastPart = sButLastPart.Substring (0, sButLastPart.Length - 1);
                        // separate last-but-one level
                        string  sLastPart2 = General.GetLastPart (sButLastPart, '/'),
                                            sButLastPart2 = sButLastPart.Substring (0, sButLastPart.Length - sLastPart2.Length);

                        // if branch or tag?
                        if ((sLastPart2 == "branches") || (sLastPart2 == "tags"))
                        {
                            sVersionPath = sLastPart2 + "/" + sLastPart;
                            return sButLastPart2;
                        }
                    }

                    sVersionPath = "";
                    return "";
                }

                public static string GetTempFileName (string sEnding = ".txt" )
                {
                    return Environment.GetEnvironmentVariable ("TEMP") + "\\smm" + Process.GetCurrentProcess ().Id.ToString () + sEnding;
                }

                public static int ShellWait (string sFilename, string sArguments, out string sStdOut, out string sStdErr )
                {
                    Process proc = new Process ();
                    proc.StartInfo.FileName = sFilename;
                    proc.StartInfo.Arguments = sArguments;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start ();

                    sStdOut = proc.StandardOutput.ReadToEnd ();
                    sStdErr = proc.StandardError.ReadToEnd ();

                    proc.WaitForExit ();
                    return proc.ExitCode;
                }

                public static string VersionPathColor (string sPath)
                {
                    if (sPath == "trunk")
                    {
                        return "Green";
                    } else
                    {
                        string sPart = GetFirstPart (sPath, '/');
                        if (sPart == "branches")
                            return "Blue";
                        else if (sPart == "tags")
                            return "Red";
                        else
                            return "Black";
                    }
                }

                // http://stackoverflow.com/questions/1266547/how-do-you-find-out-when-youve-been-loaded-via-xml-serialization
                public static void ProcessOnDeserialize (object _result)
                {
                    var type = _result != null ? _result.GetType () : null;
                    var methods = type != null ? type.GetMethods ().Where (_ => _.GetCustomAttributes (true).Any (_m => _m is OnDeserializedAttribute)) : null;
                    if (methods != null)
                    {
                        foreach (var mi in methods)
                        {
                            mi.Invoke (_result, null);
                        }
                    }
                    var properties = type != null ? type.GetProperties ().Where (_ => _.GetCustomAttributes (true).Any (_m => _m is XmlElementAttribute || _m is XmlAttributeAttribute)) : null;
                    if (properties != null)
                    {
                        foreach (var prop in properties)
                        {
                            var obj = prop.GetValue (_result, null);
                            var enumeration = obj as IEnumerable;
                            if (obj is IEnumerable)
                            {
                                foreach (var item in enumeration)
                                {
                                    ProcessOnDeserialize (item);
                                }
                            }
                            else
                            {
                                ProcessOnDeserialize (obj);
                            }
                        }
                    }
                }

                public static bool SelectTreeItem(IViewElement item, string sPath)
                {
                    if (item.Path == sPath)
                    {
                        item.IsSelected = true;
                        return true;
                    }
                    else
                    {
                        foreach (IViewElement child in item.ViewChilds)
                        {
                            if (SelectTreeItem (child, sPath))
                                return true;
                        }
                    }
                    return false;
                }
        #endregion
    }

    public interface INotifyDoubleClick
    {
        bool OnMouseDoubleClick (TreeView tv, TreeViewItem item);
    }

    public interface ISelectionValidation
    {
        bool ValidSelection
        { get; }
    }

    public interface IDataInterface : INotifyDoubleClick
    {
        string Name
        { get; }

        string Path
        { get; }
    }

    public interface IDataElement : IDataInterface
    {
        bool IsChanged
        { get; }

        Object TreeItem
        { get; set; }

        Object MainWnd
        { get; }

        IDataElement Parent
        { get; }

        IEnumerable<IDataElement> DataChilds
        { get; }
    }

    public interface IViewElement : IDataInterface
    {
        bool IsSelected
        { get; set; }

        IEnumerable<IViewElement> ViewChilds
        { get; }
    }
}