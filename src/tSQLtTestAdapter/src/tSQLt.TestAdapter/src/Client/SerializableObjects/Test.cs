using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace tSQLt.TestAdapter.Client
{
    public class Test
    {
        [XmlAttribute("classname")]
        public string ClassName;

        [XmlAttribute("name")]
        public string Name;

        public bool Failed;

        [XmlElement("failure")]
        public Failure Failure;

        [XmlElement("error")]
        public Failure Error;


    }
}
