using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace tSQLt.TestAdapter.Dacpac
{
    public static class ObjectIdentifierExtensions
    {
        public static string GetName(this ObjectIdentifier name)
        {
            return name.Parts.LastOrDefault();
        }

        public static string GetSchema(this ObjectIdentifier name)
        {
            if (name.Parts.Count == 3)
            {
                return name.Parts[1];
            }

            if (name.Parts.Count == 2)
            {
                return name.Parts[0];
            }

            return null;
        }

        public static string GetNameFullQuoted(this ObjectIdentifier name)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var part in name.Parts.Reverse())
            {
                builder.Append(part.Quote());
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(".");
                }
            }

            return builder.ToString();
        }

        public static string GetNameFullUnQuoted(this ObjectIdentifier name)
        {
            var builder = new StringBuilder();


            foreach (var part in name.Parts) //.Reverse())
            {
                builder.AppendFormat("{0}.", part.UnQuote());
            }

            var fullName = builder.ToString();
            return fullName.Substring(0, fullName.Length - 1);
        }
        public static string GetSchemaObjectName(this ObjectIdentifier name)
        {
            return string.Format("{0}.{1}", name.GetSchema(), name.GetName());
        }


        public static bool EqualsName(this SchemaObjectName source, SchemaObjectName target)
        {
            return source.BaseIdentifier.Quote() == target.BaseIdentifier.Quote() &&
                   source.SchemaIdentifier.Quote() == target.SchemaIdentifier.Quote();
        }

        public static Identifier ToIdentifier(this ObjectIdentifier source)
        {
            var name = source.GetName();

            return new Identifier
            {
                Value = name.Quote()
            };
        }

        public static SchemaObjectName ToSchemaObjectName(this ObjectIdentifier source)
        {
            var target = new SchemaObjectName();
            target.Identifiers.Add(source.GetSchema().ToScriptDomIdentifier().Quote());
            target.Identifiers.Add(source.GetName().ToScriptDomIdentifier().Quote());

            return target;
        }
    }
}