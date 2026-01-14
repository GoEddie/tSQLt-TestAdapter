using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SSDTExtensions
{
    /// <summary>
    /// Extracts object names from SQL batches using TSqlScriptDom parser
    /// </summary>
    public class SqlObjectNameExtractor
    {
        /// <summary>
        /// Extracts object names from SQL batches
        /// </summary>
        /// <param name="batches">SQL batches to parse</param>
        /// <returns>List of object names</returns>
        public List<string> ExtractObjectNames(List<string> batches)
        {
            var objectNames = new List<string>();

            foreach (var batch in batches)
            {
                try
                {
                    var parser = new TSql160Parser(true);
                    IList<ParseError> errors;

                    using (var reader = new StringReader(batch))
                    {
                        var fragment = parser.Parse(reader, out errors);

                        if (errors.Count == 0)
                        {
                            var visitor = new ObjectNameVisitor();
                            fragment.Accept(visitor);
                            objectNames.AddRange(visitor.ObjectNames);
                        }
                    }
                }
                catch
                {
                    // If parsing fails, skip this batch
                }
            }

            return objectNames;
        }

        /// <summary>
        /// Custom visitor to extract object names from CREATE/ALTER statements
        /// </summary>
        private class ObjectNameVisitor : TSqlFragmentVisitor
        {
            public List<string> ObjectNames { get; } = new List<string>();

            private string GetFullObjectName(SchemaObjectName schemaObjectName)
            {
                if (schemaObjectName == null)
                    return null;

                var parts = new List<string>();

                if (schemaObjectName.SchemaIdentifier != null)
                {
                    parts.Add(schemaObjectName.SchemaIdentifier.Value);
                }

                if (schemaObjectName.BaseIdentifier != null)
                {
                    parts.Add(schemaObjectName.BaseIdentifier.Value);
                }

                return string.Join(".", parts);
            }

            public override void ExplicitVisit(CreateProcedureStatement node)
            {
                if (node.ProcedureReference?.Name != null)
                {
                    string fullName = GetFullObjectName(node.ProcedureReference.Name);
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        ObjectNames.Add(fullName);
                    }
                }
                // Don't call base.ExplicitVisit to avoid descending into procedure body
            }

            public override void ExplicitVisit(CreateFunctionStatement node)
            {
                if (node.Name != null)
                {
                    string fullName = GetFullObjectName(node.Name);
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        ObjectNames.Add(fullName);
                    }
                }
                // Don't call base.ExplicitVisit to avoid descending into function body
            }

            public override void ExplicitVisit(CreateViewStatement node)
            {
                if (node.SchemaObjectName != null)
                {
                    string fullName = GetFullObjectName(node.SchemaObjectName);
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        ObjectNames.Add(fullName);
                    }
                }
                base.ExplicitVisit(node);
            }

            public override void ExplicitVisit(CreateTableStatement node)
            {
                if (node.SchemaObjectName != null)
                {
                    string fullName = GetFullObjectName(node.SchemaObjectName);
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        ObjectNames.Add(fullName);
                    }
                }
                base.ExplicitVisit(node);
            }

            public override void ExplicitVisit(CreateSchemaStatement node)
            {
                if (node.Name != null)
                {
                    string schemaName = node.Name.Value;
                    if (!string.IsNullOrEmpty(schemaName))
                    {
                        ObjectNames.Add(schemaName);
                    }
                }
                base.ExplicitVisit(node);
            }
        }
    }
}
