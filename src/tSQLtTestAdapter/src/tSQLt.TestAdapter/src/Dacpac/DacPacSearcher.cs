using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tSQLt.TestAdapter.Dacpac
{
    public static class DacpacHelper
    {
        public static string DllNameToDacpacName(string name) => name.Replace(".dll", ".dacpac");
    }
    public class DacPacSearcher
    {
        public void FindTestClasses()
        {

        }
    }
}
