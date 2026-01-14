using System.Xml.Serialization;

namespace tSQLt.TestAdapter.Client
{
    public class Failure
    {
        [XmlAttribute("message")]
        public string Message;

    }

    public class Error
    {
        [XmlAttribute("message")]
        public string Message;
    }
}
