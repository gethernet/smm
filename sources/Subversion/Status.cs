using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml.Serialization;

namespace smm.Subversion
{
    public class SvnXmlStatusCommit
    {
        ////////////////////////////////////////////////////////////////
        [XmlAttribute ("revision")]
        public string m_sRevision;

        [XmlElement ("author")]
        public string m_sAuthor;

        [XmlElement ("date")]
        public string m_sDate;

        [XmlElement ("commit")]
        public SvnXmlStatusCommit m_Commit;
    }

    public class SvnXmlStatusWC
    {
        ////////////////////////////////////////////////////////////////
        [XmlAttribute ("props")]
        public string m_sProps;

        [XmlAttribute ("item")]
        public string m_sItem;

        [XmlAttribute ("copied")]
        public string m_sCopied;

        [XmlAttribute ("moved-from")]
        public string m_sMovedFrom;

        [XmlAttribute ("moved-to")]
        public string m_sMovedTo;

        [XmlAttribute ("revision")]
        public string m_sRevision;
    }

    public class SvnXmlStatusEntry
    {
        ////////////////////////////////////////////////////////////////
        [XmlAttribute ("path")]
        public string m_sPath;

        [XmlElement ("wc-status")]
        public SvnXmlStatusWC m_WC;

        public bool IsModified ()
        {
            return ("modified" == m_WC.m_sItem);
        }
    }
    public class SvnXmlStatusTarget
    {
        ////////////////////////////////////////////////////////////////
        [XmlElement ("path")]
        public string m_sPath;
        [XmlElement ("entry")]
        public List<SvnXmlStatusEntry> m_Entries;
    }

    [XmlRoot("status")]
    public class SvnXmlStatus
    {
        ////////////////////////////////////////////////////////////////
        [XmlElement ("target")]
        public List<SvnXmlStatusTarget> m_Targets;

        ////////////////////////////////////////////////////////////////
        public SvnXmlStatus ()
        {
        }

        ////////////////////////////////////////////////////////////////
        public static SvnXmlStatus GetStatus (string sPath)
        {
            //MessageBox.Show("GetExternals");
            string  sStdOut,
                    sStdErr;
            General.ShellWait ("svn.exe", "status --xml \"" + sPath + "\"", out sStdOut, out sStdErr);

            if (sStdErr.Length == 0)
            {
                // https://msdn.microsoft.com/de-de/library/tz8csy73(v=vs.110).aspx
                XmlSerializer serializer = new XmlSerializer (typeof (SvnXmlStatus));
                StringReader reader = new StringReader (sStdOut);
                SvnXmlStatus status = (SvnXmlStatus)serializer.Deserialize (reader);

                return status;
            }
            else
            {
                return null;
            }
        }

        public string GetModifiedElements ()
        {
            string sFiles = "";
            foreach (SvnXmlStatusEntry entry in m_Targets[0].m_Entries)
            {
                if (entry.IsModified ())
                    sFiles = sFiles + entry.m_sPath + "\n";
            }

            return sFiles;
        }
    }
}
