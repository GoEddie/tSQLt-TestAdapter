using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace tSQLt.TestAdapter.Dacpac
{
    public static class StringExtensions
    {
        public static Identifier ToScriptDomIdentifier(this string source)
        {
            return new Identifier
            {
                Value = source
            };
        }

        public static bool IsQuoted(this string source)
        {
            return source.StartsWith("[");
        }

        public static string CorrectQuote(this string source, QuoteType type)
        {
            switch (type)
            {
                case QuoteType.NotQuoted:
                    return source.UnQuote();
                case QuoteType.SquareBracket:
                    return source.Quote();
                case QuoteType.DoubleQuote:
                    return source.SpeechQuote();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public static string SpeechQuote(this string source)
        {
            if (!source.StartsWith("\""))
                source = "\"" + source;

            if (!source.EndsWith("\""))
                source = source + "\"";

            return source;
        }

        public static string Quote(this string source)
        {
            if (!source.StartsWith("["))
                source = "[" + source;

            if (!source.EndsWith("]"))
                source = source + "]";

            return source;
        }

        public static string UnQuote(this string source)
        {
            if (source.StartsWith("["))
                source = source.Substring(1);

            if (source.EndsWith("]"))
                source = source.Substring(0, source.Length - 1);

            return source;
        }
    }
}