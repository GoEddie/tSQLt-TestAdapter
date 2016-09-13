using System;
using System.Collections.Generic;
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
    public class tSQLtTestDiscoverer : ITestDiscoverer
    {
        private static readonly object _lock = new object();
        private static readonly TestCache _tests = new TestCache();

        private static readonly List<Regex> _includePaths = new List<Regex>();
        private IMessageLogger _logger;
        private bool _debug;

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var settings = new RunSettings(discoveryContext.RunSettings);
            _logger = logger;

            _debug = settings.GetSetting("tSQLt-TestAdapter-Debug")?.ToLowerInvariant() == "true";
            
            if (string.IsNullOrEmpty(settings.GetSetting("TestDatabaseConnectionString")))
            {
                logger.SendMessage(TestMessageLevel.Informational, "No RunSettings TestDatabaseConnectionString set - will not attempt to discover tests..");
                return;
            }
            
            logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...");
            
            var includePath = new RunSettings(discoveryContext.RunSettings).GetSetting("IncludePath");
            
            lock (_lock)
            {
                GetTests(sources, discoverySink, includePath);
            }

            if (_tests != null)
                logger.SendMessage(TestMessageLevel.Informational, string.Format("tSQLt Test Adapter, searching for tests...done - {0} found", _tests.GetTests().Sum(p => p.Tests.Count)));
            else
                logger.SendMessage(TestMessageLevel.Informational, "tSQLt Test Adapter, searching for tests...done - none found");
        }

        public void SetPathFilter(string includePath)
        {
            _includePaths.Clear();

            if (!string.IsNullOrEmpty(includePath))
            {
                if (includePath.IndexOf(";", StringComparison.Ordinal) >= 0)
                {
                    foreach (var part in includePath.Split(';'))
                    {
                        Debug($"tSQLt-Test-Adapter: Adding filter: {part}");
                        _includePaths.Add(new Regex(part));
                    }
                }
                else
                {
                    Debug($"tSQLt-Test-Adapter: Adding filter: {includePath}");
                    _includePaths.Add(new Regex(includePath));
                }
            }
        }

        private void Debug(string message)
        {
            if (_debug)
                _logger.SendMessage(TestMessageLevel.Informational, message);
        }


        public  List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink, string filter)
        {
            lock (_lock)
            {
                if(!String.IsNullOrEmpty(filter))
                    SetPathFilter(filter);

                var tests = new List<TestCase>();

                foreach (var source in sources)
                {
                    if (_includePaths.Count == 0 || _includePaths.Any(p => p.IsMatch(source)))
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
                        Debug($"tSQLt-Test-Adapter Adding test case {testClass.Name}.{test.Name}");
                        if (discoverySink != null)
                        {
                            Debug($"tSQLt-Test-Adapter Adding test case {testClass.Name}.{test.Name} - SENDING TO discoverSink");
                            discoverySink.SendTestCase(testCase);
                        }
                    }

                    if (discoverySink != null)
                    {
                        var tcClass = new TestCase(testClass.Name + " TestClass", tSQLtTestExecutor.ExecutorUri, testClass.Path);

                        tcClass.CodeFilePath = testClass.Path;

                        tests.Add(tcClass);
                        Debug($"tSQLt-Test-Adapter Adding test case {testClass.Name} - SENDING test class wrapper");
                        discoverySink.SendTestCase(tcClass);
                    }
                }

                return tests;
            }
        }
    }
}