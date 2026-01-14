using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace tSQLt.TestAdapter.Client
{
    [XmlRoot("testsuites")]
    public class TestSuites
    {
        [XmlElement("testsuite")]
        public List<TestSuite> Suites { get; set; }

        public TestSuites()
        {

        }

        public bool Passed()
        {
            return Suites.All(p => p.WasSuccess());
        }

        public int TestCount()
        {
            return Suites.Sum(p => p.TestCount);
        }

        public int FailureCount()
        {
            return Suites.Sum(p => p.FailureCount);
        }

        public int ErrorCount()
        {
            return Suites.Sum(p => p.ErrorCount);
        }

        public string FailureMessages()
        {
            var messages = new StringBuilder();

            foreach (var suite in Suites)
            {
                messages.Append(suite.FailureMessages());
            }

            return messages.ToString();
        }

    }
}
