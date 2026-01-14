using System.Collections.Generic;
using tSQLt.TestAdapter.Client.Gateways;

namespace tSQLt.TestAdapter.Client
{
    /// <summary>
    /// Contains the test results along with any earlier recordsets produced during test execution
    /// </summary>
    public class TestRunResult
    {
        public TestSuites TestResults { get; set; }
        public List<ResultSetTable> EarlierResultSets { get; set; }

        public TestRunResult()
        {
            EarlierResultSets = new List<ResultSetTable>();
        }

        public TestRunResult(TestSuites testResults, List<ResultSetTable> earlierResultSets)
        {
            TestResults = testResults;
            EarlierResultSets = earlierResultSets ?? new List<ResultSetTable>();
        }
    }
}
