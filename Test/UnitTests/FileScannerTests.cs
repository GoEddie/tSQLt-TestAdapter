using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgileSQLClub.tSQLtTestController;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class FileScannerTests
    {
        [Test]
        public void FindsExtendedProperty()
        {
            var scanner = new FileScanner(new TSql130Parser(false));


            var result = 
            scanner.ScanCode(@"EXECUTE sp_addextendedproperty
 @name = N'tSQLt.TestClass'
 , @value = 1
 , @level0type = N'SCHEMA'
 , @level0name = N'MyUnitSchema';
", new ScanResults());

            Assert.AreEqual(1, result.FoundProperties.Count);
            Assert.AreEqual("MyUnitSchema", result.FoundProperties.First().SchemaName);

        }

        [Test]
        public void FindsSchema()
        {
            var scanner = new FileScanner(new TSql130Parser(false));


            var result =
            scanner.ScanCode("--select 100\r\nGO\r\ncreate      /*AAAAAAAA*/ schema [my_schema];", new ScanResults());

            Assert.AreEqual(1, result.FoundClasses.Count);
            Assert.AreEqual("my_schema", result.FoundClasses.First().Name);

        }

        [Test]
        public void FindsTestProcedure()
        {
            var scanner = new FileScanner(new TSql130Parser(false));


            var result =
            scanner.ScanCode("--select 100\r\nGO\r\ncreate    procedure [test hello there] as select 1;", new ScanResults());

            Assert.AreEqual(1, result.FoundPotentialTests.Count);
            Assert.AreEqual("test hello there", result.FoundPotentialTests.First().Name.Object);

        }

        [Test]
        public void FindsDoesNotFindNonTestProcedure()
        {
            var scanner = new FileScanner(new TSql130Parser(false));


            var result =
            scanner.ScanCode("--select 100\r\nGO\r\ncreate    procedure [blah hello there] as select 1;", new ScanResults());

            Assert.AreEqual(0, result.FoundPotentialTests.Count);
            

        }

    }
}
