using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace tSQLt.TestAdapter.Client
{
    public class TestSuite
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("tests")]
        public int TestCount { get; set; }

        [XmlAttribute("failures")]
        public int FailureCount { get; set; }

        [XmlAttribute("errors")]
        public int ErrorCount { get; set; }

        public bool WasSuccess()
        {
            return TestCount > 0 && FailureCount == 0 && ErrorCount == 0;
        }

        public TestSuite()
        {

        }

        public string FailureMessages()
        {
            var messages = new StringBuilder();
            messages.AppendLine(_errorMessage);

            foreach (var test in Tests)
            {
                if(test.Failure != null)
                    messages.AppendFormat("{0}.{1}: {2}\r\n", test.ClassName, test.Name, test.Failure.Message);

                if (test.Error != null)
                    messages.AppendFormat("{0}.{1}: {2}\r\n", test.ClassName, test.Name, test.Error.Message);

            }

            return messages.ToString();
        }

        [XmlElement("testcase")]
        public List<Test> Tests { get; set; }

        private readonly string _errorMessage;

        public TestSuite(string message)
        {
            _errorMessage = message;
        }
    }
}
