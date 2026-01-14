using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;

namespace tSQLt.TestAdapter.Dacpac
{
    public class TestSchema
    {
        public TestSchema(ObjectIdentifier name)
        {
            Name = name;
        }

        public ObjectIdentifier Name
        {
            get;
        }

        public List<TestProc> Tests = new List<TestProc>();
    }
}