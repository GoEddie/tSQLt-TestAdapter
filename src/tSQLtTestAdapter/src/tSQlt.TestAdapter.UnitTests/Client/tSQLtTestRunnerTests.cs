using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using tSQLt.TestAdapter.Client;
using tSQLt.TestAdapter.Client.Gateways;

namespace tSQlt.TestAdapter.UnitTests.Client
{
    /// <summary>
    /// Test implementation of ISqlServerGateway for unit testing
    /// NOTE: This gateway bypasses validation to simplify testing.
    /// In real usage, validation is handled by TestClassValidator.
    /// </summary>
    internal class TestSqlServerGateway : ISqlServerGateway
    {
        public string ExpectedXmlResult { get; set; }
        public TestExecutionResult ExpectedExecutionResult { get; set; }

        public string RunWithXmlResult(string query)
        {
            return ExpectedXmlResult;
        }

        public TestExecutionResult RunWithXmlResultAndCapture(string query)
        {
            return ExpectedExecutionResult;
        }

        public void RunWithNoResult(string query)
        {
            // No-op for tests
        }

        public DataReaderResult RunWithDataReader(string query)
        {
            // For testing purposes, we can't easily create a SqlDataReader
            // The validation logic would need integration tests with a real database
            throw new NotImplementedException("DataReader validation requires integration tests with a real database");
        }
    }

    [TestClass]
    public class tSQLtTestRunnerTests
    {
        private TestSqlServerGateway _gateway;
        private tSQLtTestRunner _runner;

        [TestInitialize]
        public void Setup()
        {
            _gateway = new TestSqlServerGateway();
            _runner = new tSQLtTestRunner(_gateway);
        }

        // NOTE: Tests for Run(), RunClass() with validation are skipped because
        // they require a real SqlDataReader which can't be easily mocked without
        // a mocking framework. These methods include validation logic that checks
        // the database for test existence. For full test coverage, integration
        // tests with a real database are recommended.

        [TestMethod]
        public void RunAll_ExecutesAllTestsInDatabase()
        {
            // Arrange
            var expectedXml = @"<testsuites>
                <testsuite name=""TestClass1"" tests=""2"" failures=""0"" errors=""0"">
                    <testcase classname=""TestClass1"" name=""test One"" />
                    <testcase classname=""TestClass1"" name=""test Two"" />
                </testsuite>
                <testsuite name=""TestClass2"" tests=""1"" failures=""0"" errors=""0"">
                    <testcase classname=""TestClass2"" name=""test Three"" />
                </testsuite>
            </testsuites>";

            _gateway.ExpectedXmlResult = expectedXml;

            // Act
            var result = _runner.RunAll();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Suites.Count);
            Assert.AreEqual(3, result.TestCount());
        }

        [TestMethod]
        public void Constructor_WithConnectionString_CreatesRunner()
        {
            // Act
            var runner = new tSQLtTestRunner("Server=.;Database=TestDB;Integrated Security=true");

            // Assert
            Assert.IsNotNull(runner);
        }

        [TestMethod]
        public void Constructor_WithConnectionStringAndTimeout_CreatesRunner()
        {
            // Act
            var runner = new tSQLtTestRunner("Server=.;Database=TestDB;Integrated Security=true", 60000);

            // Assert
            Assert.IsNotNull(runner);
        }

    }
}
