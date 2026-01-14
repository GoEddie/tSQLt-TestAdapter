using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tSQLt.TestAdapter.Client.Gateways;

namespace tSQLt.TestAdapter.Client.TestValidators
{
    class TestClassValidator
    {
        private readonly ISqlServerGateway _gateway;

        public TestClassValidator(ISqlServerGateway gateway)
        {
            _gateway = gateway;
        }

        public bool Validate(string className)
        {
            var query = Queries.GetQueryForValidateClass(className);
            var readerContainer = _gateway.RunWithDataReader(query);
            if (readerContainer.Reader.Read())
            {
                var schemaCount = readerContainer.Reader["schema_count"] as int?;
                var classCount = readerContainer.Reader["test_class_count"] as int?;

                readerContainer.Reader.Close();
                readerContainer.Connection.Close();

                return (schemaCount.HasValue && schemaCount.Value > 0)
                            &&
                        (classCount.HasValue && classCount.Value > 0)
                        ;
            }

            return false;

        }

        public bool Validate(string className, string testName)
        {
            var query = Queries.GetQueryForValidateTest(className, testName);
            var readerContainer = _gateway.RunWithDataReader(query);
            if (readerContainer.Reader.Read())
            {
                var schemaCount = readerContainer.Reader["schema_count"] as int?;
                var classCount = readerContainer.Reader["test_class_count"] as int?;
                var testCount = readerContainer.Reader["proc_count"] as int?;
                readerContainer.Reader.Close();
                readerContainer.Connection.Close();

                return (schemaCount.HasValue && schemaCount.Value > 0)
                            &&
                        (classCount.HasValue && classCount.Value > 0)
                            &&
                         (testCount.HasValue && testCount.Value > 0);
            }

            return false;
        }
    }
}
