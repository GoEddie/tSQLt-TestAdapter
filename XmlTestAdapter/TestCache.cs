using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AgileSQLClub.tSQLtTestController;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace tSQLtTestAdapter
{
    public class TestCache
    {
        private readonly Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>();
        private bool _haveChanges = true;
        private ScanResults _results = new ScanResults();
        private readonly FileScanner _scanner = new FileScanner(new TSql130Parser(false));
        private readonly List<TestClass> _tests = new List<TestClass>();


        public void AddPath(string path)
        {
            var date = File.GetLastWriteTimeUtc(path);
            if (!_dateCache.ContainsKey(path) || date <= _dateCache[path])
            {
                _results = _scanner.ScanCode(File.ReadAllText(path), _results, path);
                _haveChanges = true;
            }
        }

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
                            e => string.Equals(p.Name, e.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            var foundTests =
                _results.FoundPotentialTests.Where(
                    p =>
                        _results.FoundPotentialTests.Any(
                            s => string.Equals(s.Name.Schema, p.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            _tests.Clear();

            foreach (var cls in foundClasses)
            {
                var testClass = new TestClass();
                testClass.Name = cls.Name;
                testClass.Path = cls.Path;
                //testClass.Tests =
                foreach (var test in foundTests.Where(p => string.Equals(p.Name.Schema, cls.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    testClass.Tests.Add(new Test {Name = test.Name.Object, Path = test.Path, Line = test.StartLine});
                }

                if (testClass.Tests.Count > 0)
                    _tests.Add(testClass);
            }

            _results = new ScanResults();
            _haveChanges = false;

            return _tests;
        }
    }
}