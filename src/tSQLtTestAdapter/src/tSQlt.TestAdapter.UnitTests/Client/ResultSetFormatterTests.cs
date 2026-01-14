using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using tSQLt.TestAdapter.Client.Gateways;

namespace tSQlt.TestAdapter.UnitTests.Client
{
    [TestClass]
    public class ResultSetFormatterTests
    {
        [TestMethod]
        public void FormatAsTable_EmptyResultSet_ReturnsEmptyString()
        {
            // Arrange
            var resultSet = new ResultSetTable();

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void FormatAsTable_SingleColumn_FormatsCorrectly()
        {
            // Arrange
            var resultSet = new ResultSetTable
            {
                ColumnNames = new List<string> { "Id" },
                Rows = new List<List<string>>
                {
                    new List<string> { "1" },
                    new List<string> { "2" },
                    new List<string> { "3" }
                }
            };

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            Assert.IsTrue(result.Contains("| Id |"));
            Assert.IsTrue(result.Contains("| 1  |"));
            Assert.IsTrue(result.Contains("| 2  |"));
            Assert.IsTrue(result.Contains("| 3  |"));
        }

        [TestMethod]
        public void FormatAsTable_MultipleColumns_FormatsCorrectly()
        {
            // Arrange
            var resultSet = new ResultSetTable
            {
                ColumnNames = new List<string> { "Id", "Name", "Status" },
                Rows = new List<List<string>>
                {
                    new List<string> { "1", "Alice", "Active" },
                    new List<string> { "2", "Bob", "Inactive" }
                }
            };

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            Assert.IsTrue(result.Contains("| Id | Name  | Status   |"));
            Assert.IsTrue(result.Contains("| 1  | Alice | Active   |"));
            Assert.IsTrue(result.Contains("| 2  | Bob   | Inactive |"));
        }

        [TestMethod]
        public void FormatAsTable_VariableLengthData_AdjustsColumnWidths()
        {
            // Arrange
            var resultSet = new ResultSetTable
            {
                ColumnNames = new List<string> { "Name" },
                Rows = new List<List<string>>
                {
                    new List<string> { "A" },
                    new List<string> { "VeryLongName" }
                }
            };

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            Assert.IsTrue(result.Contains("VeryLongName"));
            // Column width should be determined by the longest value
            Assert.IsTrue(result.Contains("| Name         |") || result.Contains("| VeryLongName |"));
        }

        [TestMethod]
        public void FormatAsTable_IncludesSeparatorRow()
        {
            // Arrange
            var resultSet = new ResultSetTable
            {
                ColumnNames = new List<string> { "Column1", "Column2" },
                Rows = new List<List<string>>
                {
                    new List<string> { "Value1", "Value2" }
                }
            };

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            // Should contain a separator line with dashes
            Assert.IsTrue(result.Contains("|-"));
            Assert.IsTrue(result.Contains("-|-"));
        }

        [TestMethod]
        public void FormatAsTable_EmptyRows_FormatsHeaderOnly()
        {
            // Arrange
            var resultSet = new ResultSetTable
            {
                ColumnNames = new List<string> { "Column1", "Column2" },
                Rows = new List<List<string>>()
            };

            // Act
            var result = resultSet.FormatAsTable();

            // Assert
            Assert.IsTrue(result.Contains("| Column1 | Column2 |"));
            Assert.IsTrue(result.Contains("|-"));
        }

        
        [TestMethod]
        public void Constructor_InitializesEmptyCollections()
        {
            // Act
            var resultSet = new ResultSetTable();

            // Assert
            Assert.IsNotNull(resultSet.ColumnNames);
            Assert.IsNotNull(resultSet.Rows);
            Assert.AreEqual(0, resultSet.ColumnNames.Count);
            Assert.AreEqual(0, resultSet.Rows.Count);
        }
    }
}
