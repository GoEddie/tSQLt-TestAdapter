using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Web;
using tSQLt.TestAdapter.Client.Gateways;
using tSQLt.TestAdapter.Client.Parsers;
using tSQLt.TestAdapter.Client.TestValidators;

namespace tSQLt.TestAdapter.Client
{
    public class tSQLtTestRunner
    {
        private readonly ISqlServerGateway _gateway;
        private readonly TestClassValidator _testValidator;

        /// <summary>
        /// Creates a tSQLtTestRunner which is used to run tests against a Sql Server database which contains the tests to run
        /// </summary>
        /// <param name="connectionString">The connection string including the initial catalog of the database to connect to</param>
        /// <param name="runTimeout">The command timeout for running the tests, defaults to 2 minutes</param>
        public tSQLtTestRunner(string connectionString, int runTimeout = 1000 * 120)
        {
            _gateway = new SqlServerGateway(connectionString, runTimeout);
            _testValidator = new TestClassValidator(_gateway);
        }

        /// <summary>
        /// This is typically only used to test the API. Use the "tSQLtTestRunner(string connectionString)" constructor instead
        /// </summary>
        /// <param name="gateway"></param>
        public tSQLtTestRunner(ISqlServerGateway gateway /*Typically only used for testing the API*/)
        {
            _gateway = gateway;
            _testValidator = new TestClassValidator(gateway);
        }

        /// <summary>
        /// Execute the tSQLt test that is in the schema "testClass" with the name "name"
        ///
        /// For Example to run the SQLCop test "test User Aliases" you would pass in:
        ///
        ///     testClass = "SQLCop"
        ///     name      = "test User Aliases"
        ///
        /// </summary>
        /// <param name="testClass">The name of the schema the test lives in</param>
        /// <param name="name">The name of the test to run</param>
        /// <returns>TestSuites - The Results of the test</returns>
        public TestSuites Run(string testClass, string name)
        {
            if (!_testValidator.Validate(testClass, name))
            {
                return XmlParser.Get(FailureMessageXml(string.Format("The test class \"{0}\" or test name \"{1}\" could not be found or does not have the tSQLt test schema extended property", testClass, name)));
            }

            return GetResults(Queries.GetQueryForSingleTest(testClass, name));
        }

        /// <summary>
        /// Execute the tSQLt test and capture any earlier recordsets produced during execution
        /// </summary>
        /// <param name="testClass">The name of the schema the test lives in</param>
        /// <param name="name">The name of the test to run</param>
        /// <returns>TestRunResult - The test results and any earlier recordsets</returns>
        public TestRunResult RunWithCapture(string testClass, string name)
        {
            if (!_testValidator.Validate(testClass, name))
            {
                var failureXml = FailureMessageXml(string.Format("The test class \"{0}\" or test name \"{1}\" could not be found or does not have the tSQLt test schema extended property", testClass, name));
                return new TestRunResult(XmlParser.Get(failureXml), new List<Gateways.ResultSetTable>());
            }

            return GetResultsWithCapture(Queries.GetQueryForSingleTest(testClass, name));
        }

        /// <summary>
        /// Executes all of the tSQLt tests that are within the schema "testClass"
        ///
        /// For Example to run all of the SQLCop tests you would pass in:
        ///
        ///     testClass = "SQLCop"
        ///
        /// </summary>
        /// <param name="testClass">The name of the schema holding all the tests you would like to run</param>
        /// <returns></returns>
        public TestSuites RunClass(string testClass)
        {
            if (!_testValidator.Validate(testClass))
            {
                return XmlParser.Get(FailureMessageXml(string.Format("The test class \"{0}\" could not be found or does not have the tSQLt test schema extended property", testClass)));
            }

            return GetResults(Queries.GetQueryForClass(testClass), Queries.GetQueryForJustResults());
        }

        /// <summary>
        /// Executes all of the tSQLt tests that are within the database the tSQLtTestRunner is connected to
        /// </summary>
        /// <returns></returns>
        public TestSuites RunAll()
        {
            return GetResults(Queries.GetQueryForAll(), Queries.GetQueryForJustResults());
        }

        private TestSuites GetResults(string queryRun, string queryResults)
        {
            string xml = "";

            try
            {
                try
                {
                    _gateway.RunWithNoResult(queryRun);
                }
                catch (SqlException)
                {

                }

                xml = _gateway.RunWithXmlResult(queryResults);
            }
            catch (Exception ex)
            {
                xml = FailureMessageXml(ex.Message);
            }

            return XmlParser.Get(xml);
        }

        private TestSuites GetResults(string query)
        {
            string xml = "";

            try
            {
                xml = _gateway.RunWithXmlResult(query);
            }
            catch (Exception ex)
            {
                xml = FailureMessageXml(ex.Message);
            }

            return XmlParser.Get(xml);
        }

        private TestRunResult GetResultsWithCapture(string query)
        {
            try
            {
                var executionResult = _gateway.RunWithXmlResultAndCapture(query);
                var testSuites = XmlParser.Get(executionResult.XmlResults);
                return new TestRunResult(testSuites, executionResult.EarlierResultSets);
            }
            catch (Exception ex)
            {
                var xml = FailureMessageXml(ex.Message);
                return new TestRunResult(XmlParser.Get(xml), new List<Gateways.ResultSetTable>());
            }
        }

        private string FailureMessageXml(string message)
        {
            return string.Format(@"<testsuites><testsuite name=""failure"" tests=""1"" errors=""1"" failures=""1"">
                <testcase classname=""failure"" name=""message"">
                <failure message=""{0}"" />
            </testcase></testsuite></testsuites> ",  message == null ? "Unknown Failure" :  WebUtility.HtmlEncode(message.Replace("\r", "").Replace("\n", "")));

        }

    }
}
