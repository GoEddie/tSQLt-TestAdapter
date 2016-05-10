using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;


namespace tSQLtTestAdapter
{
  
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    [FileExtension(".sql")]
    public class tSQLtTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            Trace.WriteLine("ksjkjskjsakjs");
            GetTests(sources, discoverySink);
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            MessageBox.Show("lklkkl" + sources.Count());
            List<TestCase> tests = new List<TestCase>();
            foreach (string source in sources)
            {

                //XmlDocument doc = new XmlDocument();
                //doc.Load(source);

                //var testNodes = doc.SelectNodes("//Tests/Test");
                //foreach (XmlNode testNode in testNodes)
                //{
                //    XmlAttribute nameAttribute = testNode.Attributes["name"];
                //    if (nameAttribute != null && !string.IsNullOrWhiteSpace(nameAttribute.Value))
                //    {
                //        var testcase = new TestCase(nameAttribute.Value, tSQLtTestExecutor.ExecutorUri, source)
                //            {
                //                CodeFilePath = source,
                //            };

                var testCase = new TestCase("Datbase.Schema.test sgsgsgsgs", tSQLtTestExecutor.ExecutorUri, source);
                tests.Add(testCase);

                if (discoverySink != null)
                {
                    discoverySink.SendTestCase(testCase);
                }
                //else
                //        {
                //            XmlAttribute outcomeAttibute = testNode.Attributes["outcome"];
                //            TestOutcome outcome;
                //            Enum.TryParse<TestOutcome>(outcomeAttibute.Value, out outcome);
                //            testcase.SetPropertyValue(TestResultProperties.Outcome, outcome);
                //        }
                //        tests.Add(testcase);
                //    }

                    //}

            }
            return tests;
        }
    }
}