using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace tSQLt.TestAdapter.Dacpac
{
    public static class IdentifierExtensions
    {
        public static Identifier Quote(this Identifier src)
        {
            src.Value = src.Value.Quote();
            return src;
        }
    }
}