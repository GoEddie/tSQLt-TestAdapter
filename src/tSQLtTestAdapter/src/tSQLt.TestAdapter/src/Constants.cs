using System;

namespace tSQLt.TestAdapter
{
    public static class Constants
    {
        public const string ExecutorUriString = "executor://tSQLtTestExecutor/";
        public const string FileExtension = ".sql";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

    }
}