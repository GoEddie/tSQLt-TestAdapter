using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Extensions;
using Microsoft.SqlServer.Dac.Model;
using tSQLt.TestAdapter.Dacpac;

namespace TestDiscoveryConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dacpacPath = "C:\\git\\DatabaseProject\\bin\\Debug\\DatabaseProject.dacpac";

            var model = new TSqlModel(dacpacPath);
            var schemas = model.GetObjects(DacQueryScopes.UserDefined, new[] { Schema.TypeClass }).ToList();

            var testSchemas = new List<TestSchema>();

            foreach (var schema in schemas)
            {
                var referenced = schema.GetReferenced(DacQueryScopes.UserDefined);
                foreach (var sqlObject in referenced)
                {
                    Console.WriteLine(sqlObject);
                }

                var referencing = schema.GetReferencing(DacQueryScopes.UserDefined);
                foreach (var sqlObject in referencing)
                {
                    Console.WriteLine(sqlObject);
                }

                var extendedProperties = model.GetObjects(DacQueryScopes.UserDefined, ExtendedProperty.TypeClass).ToList();

                foreach (var property in extendedProperties)
                {
                    if (property.GetReferenced(ExtendedProperty.Host).Any(p => p.Name.GetName() == schema.Name.GetName()))
                    {
                        if (String.Equals(property.Name.GetName(), "tSQLt.TestClass", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine(property);
                            var testSchema = new TestSchema(schema.Name);

                            var procs = schema.GetReferencing(DacQueryScopes.UserDefined);

                            foreach (var p in procs)
                            {
                                Console.WriteLine(p);
                                if (p.Name.GetName().StartsWith("test", StringComparison.OrdinalIgnoreCase))
                                {

                                    testSchema.Tests.Add(new TestProc(p.Name));
                                }
                            }

                            testSchemas.Add(testSchema);
                        }


                    }
                }

            }
            
            Console.WriteLine(schemas);
        }
    }
}
