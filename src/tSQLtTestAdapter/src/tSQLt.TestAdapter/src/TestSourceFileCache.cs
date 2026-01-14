using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace tSQLt.TestAdapter
{
    /// <summary>
    /// Holds source file location information for a test procedure
    /// </summary>
    public class TestSourceLocation
    {
        public string SchemaName { get; set; }
        public string ProcedureName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Caches the locations of test procedures in SQL source files
    /// </summary>
    public class TestSourceFileCache
    {
        private readonly Dictionary<string, TestSourceLocation> _cache = new Dictionary<string, TestSourceLocation>(StringComparer.OrdinalIgnoreCase);
        private readonly IMessageLogger _logger;

        public TestSourceFileCache(IMessageLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Scans one or more directories for SQL files and builds the cache of test procedure locations
        /// </summary>
        /// <param name="rootPaths">Root directories to scan</param>
        public void BuildCache(IEnumerable<string> rootPaths)
        {
            if (rootPaths == null || !rootPaths.Any())
            {
                _logger.SendMessage(TestMessageLevel.Warning, "No test folders provided to build cache");
                return;
            }

            var totalSqlFiles = 0;

            foreach (var rootPath in rootPaths)
            {
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    _logger.SendMessage(TestMessageLevel.Warning, $"Test folder does not exist or is invalid: {rootPath}");
                    continue;
                }

                _logger.SendMessage(TestMessageLevel.Informational, $"Scanning SQL files in: {rootPath}");

                // Find all .sql files recursively
                var sqlFiles = Directory.GetFiles(rootPath, "*.sql", SearchOption.AllDirectories);
                _logger.SendMessage(TestMessageLevel.Informational, $"Found {sqlFiles.Length} SQL file(s) in this folder");
                totalSqlFiles += sqlFiles.Length;

                foreach (var filePath in sqlFiles)
                {
                    try
                    {
                        ParseSqlFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.SendMessage(TestMessageLevel.Warning, $"Error parsing {filePath}: {ex.Message}");
                    }
                }
            }

            _logger.SendMessage(TestMessageLevel.Informational, $"Scanned {totalSqlFiles} SQL file(s) across {rootPaths.Count()} folder(s)");
            _logger.SendMessage(TestMessageLevel.Informational, $"Cached {_cache.Count} test procedure(s)");
        }

        /// <summary>
        /// Parses a SQL file to find test procedures
        /// </summary>
        private void ParseSqlFile(string filePath)
        {
            var fileContent = File.ReadAllText(filePath);

            // Use SQL Server ScriptDom parser
            var parser = new TSql150Parser(true);
            IList<ParseError> errors;

            using (var reader = new StringReader(fileContent))
            {
                var fragment = parser.Parse(reader, out errors);

                // Visit all CREATE PROCEDURE statements
                var visitor = new ProcedureVisitor();
                fragment.Accept(visitor);

                foreach (var procedure in visitor.Procedures)
                {
                    var schemaName = procedure.SchemaName ?? "dbo";
                    var procName = procedure.ProcedureName;

                    // Check if procedure name starts with "test"
                    if (procName.StartsWith("test", StringComparison.OrdinalIgnoreCase))
                    {
                        var key = $"{schemaName}.{procName}";

                        var location = new TestSourceLocation
                        {
                            SchemaName = schemaName,
                            ProcedureName = procName,
                            FilePath = filePath,
                            LineNumber = procedure.LineNumber
                        };

                        _cache[key] = location;

                        _logger.SendMessage(TestMessageLevel.Informational,
                            $"  Cached test: [{schemaName}].[{procName}] at {filePath}:{procedure.LineNumber}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the source location for a test procedure
        /// </summary>
        /// <param name="schemaName">Schema name</param>
        /// <param name="procedureName">Procedure name</param>
        /// <returns>Source location or null if not found</returns>
        public TestSourceLocation GetLocation(string schemaName, string procedureName)
        {
            var key = $"{schemaName}.{procedureName}";
            return _cache.TryGetValue(key, out var location) ? location : null;
        }

        /// <summary>
        /// Visitor class to find CREATE PROCEDURE statements in the SQL syntax tree
        /// </summary>
        private class ProcedureVisitor : TSqlFragmentVisitor
        {
            public List<ProcedureInfo> Procedures { get; } = new List<ProcedureInfo>();

            public override void ExplicitVisit(CreateProcedureStatement node)
            {
                var schemaName = node.ProcedureReference.Name.SchemaIdentifier?.Value;
                var procName = node.ProcedureReference.Name.BaseIdentifier.Value;
                var lineNumber = node.StartLine;

                // Remove square brackets if present
                schemaName = schemaName?.Trim('[', ']');
                procName = procName?.Trim('[', ']');

                Procedures.Add(new ProcedureInfo
                {
                    SchemaName = schemaName,
                    ProcedureName = procName,
                    LineNumber = lineNumber
                });

                base.ExplicitVisit(node);
            }
        }

        private class ProcedureInfo
        {
            public string SchemaName { get; set; }
            public string ProcedureName { get; set; }
            public int LineNumber { get; set; }
        }
    }
}
