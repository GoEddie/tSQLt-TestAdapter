using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace tSQLtTestAdapter
{
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    [FileExtension(Constants.FileExtension)]
    public class XmlTestDiscoverer : ITestDiscoverer
    {
        private static readonly object _lock = new object();
        private static readonly TestCache _tests = new TestCache();

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            Debugger.Launch();

            logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...");

            lock (_lock)
            {
                GetTests(sources, discoverySink);
            }

            if(_tests!= null)
                logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, searching for tests...done - {0} found", _tests.GetTests().Sum(p=>p.Tests.Count)));
            else
                logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...done - none found");
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            lock (_lock)
            {
                var tests = new List<TestCase>();
                foreach (var source in sources)
                {
                    _tests.AddPath(source);
                }

                var testInCode = _tests.GetTests();

                foreach (var testClass in testInCode)
                {
                    foreach (var test in testClass.Tests)
                    {
                        var testCase = new TestCase(string.Format("{0}.{1}", testClass.Name, test.Name), tSQLtTestExecutor.ExecutorUri, test.Path);
                        testCase.LineNumber = test.Line;
                        testCase.CodeFilePath = test.Path;
                        
                        tests.Add(testCase);
                        
                        if (discoverySink != null)
                        {
                            discoverySink.SendTestCase(testCase);
                        }
                    }
                }

                return tests;
            }
        }
    }
}