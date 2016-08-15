using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ScanResults ScanCode(string code, ScanResults results)
        {
            
            var batches = code.Split(new[] {"\r\nGO\r\n", "\nGO\n"}, StringSplitOptions.None);

            foreach (var batch in batches)
            {
                results = AppendResults(results, batch);
            }

            return results;
        }

        private ScanResults AppendResults(ScanResults results, string batch)
        {
            //
            var reader = new StringReader(batch);
            IList<ParseError> errors;
            var fragment = _parser.Parse(reader, out errors);
            var visitor = new TestVisitor();
            fragment.Accept(visitor);
            Console.WriteLine("hjhjhjh");

            results.FoundProperties.AddRange(visitor.ExtendedProperties);
            results.FoundClasses.AddRange(visitor.Schemas);
            results.FoundPotentialTests.AddRange(visitor.Procedures);

            return results;
        }
    }

    public class TestVisitor : TSqlFragmentVisitor
    {
        public readonly List<tSQLtExtendedProperty> ExtendedProperties = new List<tSQLtExtendedProperty>();
        public readonly List<SqlSchema> Schemas = new List<SqlSchema>();
        public readonly  List<SqlProctedure> Procedures = new List<SqlProctedure>();

        public override void Visit(CreateProcedureStatement proc)
        {
            var name = new SqlObjectName();
            var son = proc.ProcedureReference.Name;
            name.Schema = son.SchemaIdentifier?.Value.UnQuote();
            name.Object = son.BaseIdentifier?.Value.UnQuote();

            if(name.Object.ToLowerInvariant().StartsWith("test"))
                Procedures.Add(new SqlProctedure(name));
            
            base.Visit(proc);
        }

        public override void Visit(CreateSchemaStatement node)
        {
            Schemas.Add(new SqlSchema(node.Name.Value.UnQuote()));

            base.Visit(node);
        }

        public override void Visit(ExecuteStatement exec)
        {
            if (exec.ExecuteSpecification == null)
                return;

            if (!(exec.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference))
                return;
            
            var refereced = (ExecutableProcedureReference) exec.ExecuteSpecification.ExecutableEntity;

            if (refereced.ProcedureReference.ProcedureReference != null && refereced.ProcedureReference.ProcedureReference.Name != null &&
                refereced.ProcedureReference.ProcedureReference.Name.BaseIdentifier.Value.IndexOf(
                    "sp_addextendedproperty", StringComparison.OrdinalIgnoreCase) > -1)
            {

                var propertyValue =
                    refereced.Parameters.FirstOrDefault(
                        p =>
                            (p.ParameterValue as StringLiteral)?.Value.IndexOf("tSQLt.TestClass",
                                StringComparison.OrdinalIgnoreCase) > -1);

                if (propertyValue == null)
                    return;

                var schemaName =((refereced.Parameters.FirstOrDefault(p => p.Variable.Name.ToLowerInvariant() == "@level0name"))?.ParameterValue as StringLiteral)?.Value;
                if(!String.IsNullOrEmpty(schemaName))
                    ExtendedProperties.Add(new tSQLtExtendedProperty(schemaName.UnQuote()));        
            }
      
        }

    }


    public class ScanResults
    {
        public List<SqlSchema> FoundClasses = new List<SqlSchema>();
        public List<SqlProctedure> FoundPotentialTests = new List<SqlProctedure>();
        public List<tSQLtExtendedProperty> FoundProperties = new List<tSQLtExtendedProperty>();
    }

    public class SqlObjectName
    {
        public string Schema;
        public string Object;
    }

    public class SqlProctedure
    {
        public SqlProctedure(SqlObjectName name)
        {
            Name = name;
        }
        public SqlObjectName Name;
        
    }

    public class SqlSchema
    {
        public SqlSchema(string schemaName)
        {
            Name = schemaName;
        }

        public string Name;
        
    }

    public class tSQLtExtendedProperty
    {
        public tSQLtExtendedProperty(string schemaName)
        {
            SchemaName = schemaName;
        }

        public string SchemaName;
    }


    public class TestFinder
    {
        private readonly TSqlParser _parser;
        private readonly List<string> _filePaths;
        private readonly string _lookupPath;

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
                var scanner= new FileScanner(_parser);
                results = scanner.ScanCode(File.ReadAllText(path), results);
            }

            var foundClasses =
                results.FoundClasses.Where(
                    p =>
                        results.FoundPotentialTests.Any(
                            e => String.Equals(p.Name, e.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            var foundTests =
                results.FoundPotentialTests.Where(
                    p =>
                        results.FoundPotentialTests.Any(
                            s => String.Equals(s.Name.Schema, p.Name.Schema, StringComparison.OrdinalIgnoreCase)));

            return classes;
        }

    }

    public class TestClasses
    {
    }

    public class TestClass
    {
        public string Name;
        public List<Test> Tests = new List<Test>();
    }

    public class Test
    {
        public string Name;

    }
}
