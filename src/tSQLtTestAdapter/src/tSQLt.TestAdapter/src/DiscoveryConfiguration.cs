using System.Collections.Generic;

namespace tSQLt.TestAdapter
{
    /// <summary>
    /// Configuration data for test discovery
    /// </summary>
    public class DiscoveryConfiguration
    {
        /// <summary>
        /// List of DACPAC file paths to search for tests
        /// </summary>
        public List<string> DacpacSources { get; set; }

        /// <summary>
        /// Root test folders containing SQL source files
        /// </summary>
        public List<string> TestFolders { get; set; }
    }
}
