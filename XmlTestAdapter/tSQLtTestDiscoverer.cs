using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static readonly List<Regex> _includePaths = new List<Regex>();
        private static IMessageLogger _logger;


        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            if (string.IsNullOrEmpty(new RunSettings(discoveryContext.RunSettings).GetSetting("TestDatabaseConnectionString")))
            {
                logger.SendMessage(TestMessageLevel.Informational, "No RunSettings TestDatabaseConnectionString set - will not attempt to discover tests..");
                return;
            }
            
            logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...");
            
            var includePath = new RunSettings(discoveryContext.RunSettings).GetSetting("IncludePath");
            SetPathFilter(includePath, logger);

            lock (_lock)
            {
                GetTests(sources, discoverySink);
            }

            if (_tests != null)
                logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, searching for tests...done - {0} found", _tests.GetTests().Sum(p => p.Tests.Count)));
            else
                logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...done - none found");

            _logger = logger;
        }

        public static void SetPathFilter(string includePath, IMessageLogger logger)
        {
            try
            {
                _includePaths.Clear();

                if (!string.IsNullOrEmpty(includePath))
                {
                    if (includePath.IndexOf(";", StringComparison.Ordinal) >= 0)
                    {
                        foreach (var part in includePath.Split(';'))
                        {
                            logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, adding filter...- {0}", part));
                            _includePaths.Add(new Regex(part));
                        }
                    }
                    else
                    {
                        logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, adding filter...- {0}", includePath));
                        _includePaths.Add(new Regex(includePath));
                    }
                }
            }catch(Exception e)
            {
                logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, *ERROR* adding filter...- {0}", includePath));
            }
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            lock (_lock)
            {
                
                var tests = new List<TestCase>();

                foreach (var source in sources)
                {
                    if (_includePaths.Count == 0 || _includePaths.Any(p => p.IsMatch(source)))
                        _tests.AddPath(source);
                }
                                
                var testInCode = _tests.GetTests(_logger);

                
                                
                foreach (var testClass in testInCode)
                {
                    foreach (var test in testClass.Tests)
                    {
                        
                        var testCase = new TestCase(string.Format("Discovered tSQLt Tests.test.{0}", test.Name.Substring(4).Trim()), tSQLtTestExecutor.ExecutorUri, testClass.Path);
                        testCase.DisplayName = string.Format("{0}.{1}", testClass.Name, test.Name);
                        testCase.LineNumber = test.Line;
                        testCase.CodeFilePath = test.Path;                        
                        
                        discoverySink.SendTestCase(testCase);
                        tests.Add(testCase);

                        if (discoverySink != null)
                        {
                            discoverySink.SendTestCase(testCase);
                        }
                    }

                    //if (discoverySink != null)
                    //{
                    //    var tcClass = new TestCase("Test Class." + testClass.Name, tSQLtTestExecutor.ExecutorUri, testClass.Path);

                    //    tcClass.CodeFilePath = testClass.Path;
                    //    tests.Add(tcClass);
                        
                    //    discoverySink.SendTestCase(tcClass);
                    //}
                }

                return tests;
            }
        }
    }
}