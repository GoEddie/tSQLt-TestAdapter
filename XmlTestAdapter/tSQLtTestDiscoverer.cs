using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using AgileSQLClub.tSQLtTestController;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;


namespace tSQLtTestAdapter
{
  
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    [FileExtension(Constants.FileExtension)]
    public class XmlTestDiscoverer : ITestDiscoverer
    {
        private static readonly  object _lock = new object();

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            lock (_lock)
            {
                //System.Diagnostics.Debugger.Launch();
                Trace.WriteLine("ksjkjskjsakjs");

                GetTests(sources, discoverySink);
            }
        }
        static TestCache _tests = new TestCache();

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            lock (_lock)
            {
                List<TestCase> tests = new List<TestCase>();
                foreach (string source in sources)
                {
                    _tests.AddPath(source);
                }

                var testInCode = _tests.GetTests();
                foreach (var testClass in testInCode)
                {
                    foreach (var test in testClass.Tests)
                    {
                        var source = sources.FirstOrDefault();

                        var testCase = new TestCase(string.Format("{0}.{1}", testClass.Name, test.Name), tSQLtTestExecutor.ExecutorUri, source);
                        
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

    public class TestCache
    {
       private FileScanner _scanner = new FileScanner(new TSql130Parser(false));
       

        public void AddPath(string path)
        {
           
            var date = File.GetLastWriteTimeUtc(path);
            if (!_dateCache.ContainsKey(path) || date <= _dateCache[path])
            {
                _results = _scanner.ScanCode(File.ReadAllText(path), _results);
                _haveChanges = true;
            }
        }

        private readonly Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>();
        private ScanResults _results = new ScanResults();
        private bool _haveChanges = true;
        private List<TestClass> _tests = new List<TestClass>();

        public List<TestClass> GetTests()
        {
            
            if (!_haveChanges)
            {
                return _tests;
            }

            var classes = new List<TestClass>();


            var foundClasses =
              _results.FoundClasses.Where(
                  p =>
                      _results.FoundPotentialTests.Any(
                          e => String.Equals(p.Name, e.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            var foundTests =
                _results.FoundPotentialTests.Where(
                    p =>
                        _results.FoundPotentialTests.Any(
                            s => String.Equals(s.Name.Schema, p.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            _tests.Clear();

            foreach (var cls in foundClasses)
            {
                var testClass = new TestClass();
                testClass.Name = cls.Name;
                //testClass.Tests =
                foreach (
                    var test in
                        foundTests.Where(p => String.Equals(p.Name.Schema, cls.Name, StringComparison.OrdinalIgnoreCase))
                    )
                {
                    testClass.Tests.Add(new Test() {Name = test.Name.Object});
                }
             
                if(testClass.Tests.Count > 0)
                    _tests.Add(testClass);
                           
            }
            

            return _tests;
        }

    }

}
   
