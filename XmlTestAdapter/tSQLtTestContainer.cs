using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace tSQLtTestAdapter
{
    public class tSQLtTestContainer:ITestContainer
    {
        private ITestContainerDiscoverer discoverer;
        public tSQLtTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri)
            :this(discoverer, source, executorUri, Enumerable.Empty<Guid>())
        {}

        public tSQLtTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri, IEnumerable<Guid> debugEngines)
        {
            this.Source = source;
            this.ExecutorUri = executorUri;
            this.DebugEngines = debugEngines;
            this.discoverer = discoverer;
            this.TargetFramework = FrameworkVersion.None;
            this.TargetPlatform = Architecture.AnyCPU;
            this.timeStamp = GetTimeStamp();
        }

        private tSQLtTestContainer(tSQLtTestContainer copy)
            : this(copy.discoverer, copy.Source, copy.ExecutorUri)
        {
            this.timeStamp = copy.timeStamp;
        }

        private DateTime GetTimeStamp()
        {
            if (!String.IsNullOrEmpty(this.Source) && File.Exists(this.Source))
            {
                return File.GetLastWriteTime(this.Source);
            }
            else
            {
                return DateTime.MinValue;
            }

        }

        private readonly DateTime timeStamp;

        public  string Source { get; set; }
        public  Uri ExecutorUri { get; set; }
        public  IEnumerable<Guid> DebugEngines { get; set; }
        public FrameworkVersion TargetFramework { get; set; }
        public Architecture TargetPlatform { get; set; }
        public override string ToString()
        {
 	        return this.ExecutorUri.ToString() + "/"+this.Source;
            //return this.ExecutorUri.ToString();
        }
        public IDeploymentData DeployAppContainer() {return null;}
        public bool IsAppContainerTestContainer { get {return false;}}
        public ITestContainerDiscoverer Discoverer { get { return discoverer; } }

        public int CompareTo(ITestContainer other)
        {
            var testContainer = other as tSQLtTestContainer;
            if (testContainer == null)
            {
                return -1;
            }

            var result = String.Compare(this.Source, testContainer.Source, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            return this.timeStamp.CompareTo(testContainer.timeStamp);
        }

        public ITestContainer Snapshot()
        {
            return new tSQLtTestContainer(this);
        }
    }
}
