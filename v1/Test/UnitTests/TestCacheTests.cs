using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgileSQLClub.tSQLtTestController;
using Moq;
using NUnit.Framework;
using tSQLtTestAdapter;

namespace UnitTests
{
    [TestFixture]
    public class TestCacheTests
    {
        [Test]
        public void DiscardsDuplicates()
        {
            var mockScanner = new Mock<FileScanner>();
            
            mockScanner.Setup(p => p.ScanCode(It.IsAny<string>(), It.IsAny<ScanResults>(), It.IsAny<string>())).Returns((
                string a, ScanResults result, string b) =>
            {
                result.FoundClasses.Add(new SqlSchema("Schema", "PathA"));
                result.FoundClasses.Add(new SqlSchema("Schema", "PathB"));
                result.FoundClasses.Add(new SqlSchema("Schema", "PathB"));

                result.FoundPotentialTests.Add(new SqlProcedure(new SqlObjectName() {Object = "test", Schema = "Schema"}, "Patha", 0, 100, 1));
                result.FoundPotentialTests.Add(new SqlProcedure(new SqlObjectName() { Object = "test", Schema = "Schema" }, "Pathb", 0, 100, 1));
                return result;
            });

            var mockFileReader = new Mock<IFileReader>();
            mockFileReader.Setup(p => p.ReadAll(It.IsAny<string>())).Returns("Blah");
            mockFileReader.Setup(p => p.GetLastWriteTimeUtc(It.IsAny<string>())).Returns(DateTime.MaxValue);
            
            var testCache = new TestCache(mockScanner.Object, mockFileReader.Object);
            testCache.AddPath("path");
            Assert.AreEqual(1, testCache.GetTests().Count);
        }
    }
}
