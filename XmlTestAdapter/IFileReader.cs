using System;

namespace tSQLtTestAdapter
{
    public interface IFileReader
    {
        DateTime GetLastWriteTimeUtc(string path);
        string ReadAll(string path);
    }
}