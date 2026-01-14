using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;

namespace tSQLt.TestAdapter.Dacpac
{
    public class TestProc
    {
        public TestProc(ObjectIdentifier name, SourceInformation si = null)
        {
            Name = name;
            Si = si;
        }
        public ObjectIdentifier Name { get; }
        public SourceInformation Si { get; }
    }
}