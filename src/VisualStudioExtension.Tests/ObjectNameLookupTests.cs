using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSDTExtensions;
using Xunit;
using Xunit.Abstractions;

namespace VisualStudioExtension.Tests
{
    
    public class ObjectNameLookupTests
    {
        private readonly ITestOutputHelper _output;

        public ObjectNameLookupTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestNmesAreFound()
        {
            var extractor = new SqlObjectNameExtractor();
            var batches = new List<string>()
            {
                "CREATE VIEW abcV AS SELECT 1", "CREATE VIEW [abcV2] AS SELECT 1", "CREATE VIEW [dbo].[abcV3] AS SELECT 1", "CREATE PROC dbo.[abcP] AS SELECT 1", "CREATE SCHEMA ABCS AUTHORIZATION DBO"};

            var names= extractor.ExtractObjectNames(batches);

            var expected = new List<string>()
            {
                "abcV", "abcV2", "dbo.abcV3", "dbo.abcP", "ABCS"
            };

            Assert.Equal(expected, names);

        }

        [Fact]
        public void Test_ChildCreatesAreNotIncluded()
        {
            var extractor = new SqlObjectNameExtractor();
            var batches = new List<string>()
            {
                "CREATE PROCEDURE [abc] AS CREATE TABLE #ABV(ab int)"};

            var names = extractor.ExtractObjectNames(batches);

            var expected = new List<string>()
            {
                "abc"
            };

            Assert.Equal(expected, names);

        }

        [Fact]
        public void Test_FindsNameForQuery()
        {
            var extractor = new SqlObjectNameExtractor();
            var batches = new List<string>()
            {
                @"CREATE PROCEDURE [Some_Tests].[test this is a test that fails]
AS
	select * from sys.objects;
	exec tSQLt.Fail 'uh oh';
RETURN 0
"
            };

            var names = extractor.ExtractObjectNames(batches);

            var expected = new List<string>()
            {
                "[Some_Tests].[test this is a test that fails]"
            };

            Assert.Equal(expected, names);
        }
    }
}
