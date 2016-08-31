using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace AgileSQLClub.tSQLtTestController
{
    public class FileScanner
    {
        private readonly TSqlParser _parser;

        public FileScanner(TSqlParser parser)
        {
            _parser = parser;
        }

        public ScanResults ScanCode(string code, ScanResults results, string path)
        {
            var batches = code.Split(new[] {"\r\nGO\r\n", "\nGO\n"}, StringSplitOptions.None);
            var offset = 0;
            var lineOffset = 0;

            foreach (var batch in batches)
            {
                results = AppendResults(results, batch, path, offset, lineOffset); //this won't be exact, depending which split option is used we will be 4 or 6 chars off... :(
                offset += batch.Length;
                lineOffset = batch.Split('\n').Length;
            }

            return results;
        }

        private ScanResults AppendResults(ScanResults results, string batch, string path, int offset, int lineOffset)
        {
            var reader = new StringReader(batch);
            IList<ParseError> errors;
            var fragment = _parser.Parse(reader, out errors);
            var visitor = new TestVisitor(path, offset, lineOffset);
            fragment.Accept(visitor);

            results.FoundProperties.AddRange(visitor.ExtendedProperties);
            results.FoundClasses.AddRange(visitor.Schemas);
            results.FoundPotentialTests.AddRange(visitor.Procedures);

            return results;
        }
    }

    public class TestVisitor : TSqlFragmentVisitor
    {
        private readonly int _lineOffset;
        private readonly int _offset;
        private readonly string _path;
        public readonly List<tSQLtExtendedProperty> ExtendedProperties = new List<tSQLtExtendedProperty>();
        public readonly List<SqlProcedure> Procedures = new List<SqlProcedure>();
        public readonly List<SqlSchema> Schemas = new List<SqlSchema>();

        public TestVisitor(string path, int offset, int lineOffset)
        {
            _path = path;
            _offset = offset;
            _lineOffset = lineOffset;
        }

        public override void Visit(CreateProcedureStatement proc)
        {
            var name = new SqlObjectName();
            var son = proc.ProcedureReference.Name;
            name.Schema = son.SchemaIdentifier?.Value.UnQuote();
            name.Object = son.BaseIdentifier?.Value.UnQuote();

            if (name.Object.ToLowerInvariant().StartsWith("test"))
                Procedures.Add(new SqlProcedure(name, _path, _offset + proc.StartOffset, proc.FragmentLength, _lineOffset + proc.StartLine));

            base.Visit(proc);
        }

        public override void Visit(CreateSchemaStatement node)
        {
            Schemas.Add(new SqlSchema(node.Name.Value.UnQuote(), _path));

            base.Visit(node);
        }

        public override void Visit(ExecuteStatement exec)
        {
            if (exec.ExecuteSpecification == null)
                return;

            if (!(exec.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference))
                return;

            var refereced = (ExecutableProcedureReference) exec.ExecuteSpecification.ExecutableEntity;

            if (refereced.ProcedureReference.ProcedureReference != null && refereced.ProcedureReference.ProcedureReference.Name != null && refereced.ProcedureReference.ProcedureReference.Name.BaseIdentifier.Value.IndexOf("sp_addextendedproperty", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var propertyValue =
                    refereced.Parameters.FirstOrDefault(
                        p =>
                            (p.ParameterValue as StringLiteral)?.Value.IndexOf("tSQLt.TestClass",
                                StringComparison.OrdinalIgnoreCase) > -1);

                if (propertyValue == null)
                    return;

                var schemaName = (refereced.Parameters.FirstOrDefault(p => p.Variable?.Name.ToLowerInvariant() == "@level0name")?.ParameterValue as StringLiteral)?.Value;
                if (!string.IsNullOrEmpty(schemaName))
                    ExtendedProperties.Add(new tSQLtExtendedProperty(schemaName.UnQuote()));
            }

            var name = refereced.ProcedureReference.ProcedureReference?.Name;
            if (name == null)
                return;

            if (name.SchemaIdentifier?.Value.ToLowerInvariant() == "tsqlt" && name.BaseIdentifier?.Value.ToLowerInvariant() == "newtestclass")
            {
                var parameterValue = (refereced.Parameters.FirstOrDefault()?.ParameterValue as StringLiteral)?.Value;

                if (!string.IsNullOrEmpty(parameterValue))
                {
                    ExtendedProperties.Add(new tSQLtExtendedProperty(parameterValue.UnQuote()));
                    Schemas.Add(new SqlSchema( parameterValue.UnQuote(),_path));
                }        
            }
            
        }
    }


    public class ScanResults
    {
        public List<SqlSchema> FoundClasses = new List<SqlSchema>();
        public List<SqlProcedure> FoundPotentialTests = new List<SqlProcedure>();
        public List<tSQLtExtendedProperty> FoundProperties = new List<tSQLtExtendedProperty>();
    }

    public class SqlObjectName
    {
        public string Object;
        public string Schema;
    }

    public class SqlProcedure
    {
        public SqlObjectName Name;

        public SqlProcedure(SqlObjectName name, string path, int startPos, int endPos, int startLine)
        {
            Name = name;
            Path = path;
            StartPos = startPos;
            EndPos = endPos;
            StartLine = startLine;
        }

        public string Path { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public int StartLine { get; set; }
    }

    public class SqlSchema
    {
        public string Name;

        public string Path;

        public SqlSchema(string schemaName, string path)
        {
            Name = schemaName;
            Path = path;
        }
    }

    public class tSQLtExtendedProperty
    {
        public string SchemaName;

        public tSQLtExtendedProperty(string schemaName)
        {
            SchemaName = schemaName;
        }
    }


    public class TestFinder
    {
        private readonly List<string> _filePaths;
        private readonly string _lookupPath;
        private readonly TSqlParser _parser;

        public TestFinder(TSqlParser parser, List<string> filePaths)
        {
            _parser = parser;
            _filePaths = filePaths;
        }

        public List<TestClass> GetTests()
        {
            var classes = new List<TestClass>();
            var results = new ScanResults();

            foreach (var path in _filePaths)
            {
                var scanner = new FileScanner(_parser);
                results = scanner.ScanCode(File.ReadAllText(path), results, path);
            }

            var foundClasses =
                results.FoundClasses.Where(
                    p =>
                        results.FoundPotentialTests.Any(
                            e => string.Equals(p.Name, e.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            var foundTests =
                results.FoundPotentialTests.Where(
                    p =>
                        results.FoundPotentialTests.Any(
                            s => string.Equals(s.Name.Schema, p.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            return classes;
        }
    }

    public class TestClasses
    {
    }

    public class TestClass
    {
        public string Name;
        public string Path;
        public List<Test> Tests = new List<Test>();
    }

    public class Test
    {
        public string Name;
        public string Path;
        public int Line { get; set; }
    }
}