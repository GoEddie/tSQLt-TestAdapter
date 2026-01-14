using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgileSQLClub.tSQLtTestController;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace UnitTests
{
    [TestFixture]
    public class tSQLtTestFinderTests
    {
        private readonly TSqlParser _parser = new TSql140Parser(true);

        
        [Test]
        [TestCase(TSqlParserBuilder.SqlServerVersion.Sql90)]
        public void Finds_Extended_Event_Procedure(TSqlParserBuilder.SqlServerVersion version)
        {

            var parser = TSqlParserBuilder.BuildNew(version, false);

            var script = @"EXECUTE sp_addextendedproperty
                            @name = N'tSQLt.TestClass'
                            , @value = 1
                            , @level0type = N'SCHEMA'
                            , @level0name = N'MyUnitSchema';";

            var scanner = new FileScanner(parser);
            scanner.ScanCode(script, new ScanResults(), "path");
        }
        [Test]

        public void Finds_Extended_Event_Procedure_With_Exec()
        {
            var script = @"EXEC sp_addextendedproperty
                            @name = N'tSQLt.TestClass'
                            , @value = 1
                            , @level0type = N'SCHEMA'
                            , @level0name = N'MyUnitSchema';";

            var scanner = new FileScanner(_parser);
            scanner.ScanCode(script, new ScanResults(), "path");
        }


        [Test]
        public void Finds_Extended_Event_Procedure_In_Brackets()
        {
            var script = @"EXECUTE [sp_aDDextendedproperty]
                            @name = N'tSQLt.TestClass'
                            , @value = 1
                            , @level0type = N'SCHEMA'
                            , @level0name = N'MyUnitSchema';";


        }

        [Test]
        public void Finds_Classes_Registered_With_NewTestClass()
        {
            var script = @"tSQLt.NewTestClass @ClassName = 'class name'";

            var scanner = new FileScanner(_parser);
            var results = scanner.ScanCode(script, new ScanResults(), "path");
            Assert.True(results.FoundClasses.Any(p=>p.Name == "class name"));
            Assert.True(results.FoundProperties.Any(p => p.SchemaName == "class name"));
        }

    }
}
