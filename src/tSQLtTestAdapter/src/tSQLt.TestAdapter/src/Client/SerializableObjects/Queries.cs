using System;

namespace tSQLt.TestAdapter.Client
{
    static class Queries
    {
        public static string GetQueryForAll()
        {
            return "exec tSQLt.RunAll";
        }


        public static string GetQueryForJustResults()
        {
            return "exec tSQLt.XmlResultFormatter";
        }

        public static string GetQueryForSingleTest(string testClass, string name)
        {
            testClass = QuoteName(testClass);
            name = QuoteName(name);

            return String.Format("exec tSQLt.RunWithXmlResults '{0}.{1}'", testClass, name);
        }

        public static string GetQueryForClass(string testClass)
        {
            testClass = QuoteName(testClass);

            return String.Format("exec tSQLt.RunTestClass '{0}'", testClass);
        }
        public static string GetQueryForValidateClass(string testClass)
        {
            return string.Format(@"declare @schema_name nvarchar(255) = '{0}'

declare @schema_count int = (select count(*) from sys.schemas where name = @schema_name);
declare @test_class_count int = (select count(*) from sys.extended_properties ep join sys.schemas s on ep.major_id = s.schema_id
	where ep.class_desc = 'SCHEMA' and ep.name = 'tSQLt.TestClass' and s.name = @schema_name);

select @schema_count schema_count, @test_class_count test_class_count", testClass.UnQuote());

        }

        public static string GetQueryForValidateTest(string testClass, string testName)
        {
            return string.Format(@"
            declare @schema_name nvarchar(255) = '{0}'
declare @test_name nvarchar(255) = '{1}'

declare @schema_count int = (select count(*) from sys.schemas where name = @schema_name);
declare @test_class_count int = (select count(*) from sys.extended_properties ep join sys.schemas s on ep.major_id = s.schema_id
    where ep.class_desc = 'SCHEMA' and ep.name = 'tSQLt.TestClass' and s.name = @schema_name);

declare @proc_count int = coalesce((select object_id(@schema_name + '.' + @test_name)), -1)


select @schema_count schema_count, @test_class_count test_class_count, @proc_count proc_count
", testClass.UnQuote(), testName.UnQuote());

        }

        private static string QuoteName(string name)
        {
            if (!name.StartsWith("["))
                name = '[' + name;

            if (!name.EndsWith("]"))
                name = name + ']';

            return name;
        }
    }
}
