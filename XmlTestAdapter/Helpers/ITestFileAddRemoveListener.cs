using System;

namespace tSQLtTestAdapter.Helpers
{
    public interface ITestFileAddRemoveListener
    {
        event EventHandler<TestFileChangedEventArgs> TestFileChanged;
        void StartListeningForTestFileChanges();
        void StopListeningForTestFileChanges();
    }
}