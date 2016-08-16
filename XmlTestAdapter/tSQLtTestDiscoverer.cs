using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using tSQLtTestAdapter.Helpers;

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
            
            logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...");

            var includePath = new RunSettings(discoveryContext.RunSettings).GetSetting("IncludePath");
            if (!String.IsNullOrEmpty(includePath))
            {
                SetPathFilter(includePath);
            }

            lock (_lock)
            {
                GetTests(sources, discoverySink);
            }

            if(_tests!= null)
                logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, searching for tests...done - {0} found", _tests.GetTests().Sum(p=>p.Tests.Count)));
            else
                logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...done - none found");
        }

        private static List<Regex> _includePaths = new List<Regex>();

        private void SetPathFilter(string includePath)
        {
            if (includePath.IndexOf(";") >= 0)
            {
                foreach (var part in includePath.Split(';'))
                {
                    _includePaths.Add(new Regex(part));
                }
            }
            else
            {
                _includePaths.Add(new Regex(includePath));
            }
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            lock (_lock)
            {
                

                var tests = new List<TestCase>();
                foreach (var source in sources)
                {
                    if(_includePaths.Count == 0 || _includePaths.Any(p=>p.IsMatch(source)))
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