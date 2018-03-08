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
    public class SvnXmlInfoRepository
    {
        [XmlElement ("root")]
        public string m_sRoot;

        [XmlElement ("uuid")]
        public string m_sUUID;        
    }

    public class SvnXmlInfoWC
    {
        [XmlElement ("wcroot-abspath")]
        public string m_sAbsPath;

        [XmlElement ("schedule")]
        public string m_sSchedule;

        [XmlElement ("depth")]
        public string m_sDepth;

        [XmlElement ("text-updated")]
        public string m_sUpdated;

        [XmlElement ("checksum")]
        public string m_sFileChecksum;
    }

    public class SvnXmlInfoCommit
    {
        [XmlAttribute ("revision")]
        public string m_sRevision;

        [XmlElement ("author")]
        public string m_sAuthor;

        [XmlElement ("date")]
        public string m_sDate;
    }

    public class SvnXmlInfoEntry
    {
        [XmlAttribute ("kind")]
        public string m_sKind;
        
        [XmlAttribute ("path")]
        public string m_sPath;
        
        [XmlAttribute ("revision")]
        public string m_sRevision;

        [XmlElement ("url")]
        public string m_sURL;

        [XmlElement ("relative-url")]
        public string m_sRelativeURL;

        [XmlElement ("repository")]
        public SvnXmlInfoRepository m_Repos;

        [XmlElement ("wc-info")]
        public SvnXmlInfoWC m_WC;

        [XmlElement ("commit")]
        public SvnXmlInfoCommit m_Commit;
    }

    [XmlRoot("info")]
    public class SvnXmlInfo
    {
        ////////////////////////////////////////////////////////////////
        [XmlElement ("entry")]
        public List<SvnXmlInfoEntry> m_Entries;

        ////////////////////////////////////////////////////////////////
        public SvnXmlInfo ()
        {
            m_Entries = new List<SvnXmlInfoEntry> ();
        }

        ////////////////////////////////////////////////////////////////
        public static SvnXmlInfo GetInfo (string sPath)
        {
            //MessageBox.Show("GetExternals");
            string  sStdOut,
                    sStdErr;
            General.ShellWait ("svn.exe", "info --xml \"" + sPath + "\"", out sStdOut, out sStdErr);

            if (sStdErr.Length == 0)
            {
                // https://msdn.microsoft.com/de-de/library/tz8csy73(v=vs.110).aspx
                XmlSerializer serializer = new XmlSerializer (typeof (SvnXmlInfo));
                StringReader reader = new StringReader (sStdOut);
                SvnXmlInfo info = (SvnXmlInfo)serializer.Deserialize (reader);

                return info;
            }
            else
            {
                return null;
            }
        }
    }
}
