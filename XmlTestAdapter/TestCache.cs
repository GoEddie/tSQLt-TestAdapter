using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AgileSQLClub.tSQLtTestController;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace tSQLtTestAdapter
{
    public class TestCache
    {
        private readonly Dictionary<string, DateTime> _dateCache = new Dictionary<string, DateTime>();
        private bool _haveChanges = true;
        private ScanResults _results = new ScanResults();
        private readonly FileScanner _scanner;
        private readonly List<TestClass> _tests = new List<TestClass>();
        private readonly IFileReader _fileReader;
        public TestCache()
        {
            _scanner = new FileScanner(new TSql130Parser(false));
            _fileReader = new FileReader();
        }

        public TestCache(FileScanner probablyAMock, IFileReader anotherMock)
        {
            _scanner = probablyAMock;
            _fileReader = anotherMock;
        }


        public void AddPath(string path)
        {
            var date = _fileReader.GetLastWriteTimeUtc(path);
            if (!_dateCache.ContainsKey(path) || date <= _dateCache[path])
            {
                _results = _scanner.ScanCode(_fileReader.ReadAll(path), _results, path);
                _haveChanges = true;
            }
        }

        public List<TestClass> GetTests(IMessageLogger logger = null)
        {
            if (!_haveChanges)
            {
                return _tests;
            }
            
            var foundClasses = new List<SqlSchema>();

            foreach (var clazz in 
                _results.FoundClasses.Where(
                    p =>
                        _results.FoundPotentialTests.Any(
                            e => string.Equals(p.Name, e.Name.Schema, StringComparison.OrdinalIgnoreCase))))
            {
                if (foundClasses.All(p => p.Name != clazz.Name))
                {
                    foundClasses.Add(clazz);
                }
            }
           

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

    public class FileReader : IFileReader
    {
        public FileReader()
        {
            
        }


        public virtual string ReadAll(string path)
        {
            return File.ReadAllText(path);
        }

        public virtual DateTime GetLastWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(path);
        }
    }
}