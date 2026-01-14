using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.Linq;

namespace tSQlt.TestAdapter.UnitTests
{
    /// <summary>
    /// Simple test implementation of IRunSettings for unit testing
    /// </summary>
    internal class TestRunSettings : IRunSettings
    {
        public TestRunSettings(string settingsXml)
        {
            SettingsXml = settingsXml;
        }

        public string SettingsXml { get; private set; }

        public ISettingsProvider GetSettings(string settingsName)
        {
            throw new System.NotImplementedException();
        }
    }

    [TestClass]
    public class RunSettingsHelperTests
    {
        private IRunSettings CreateRunSettings(string xml)
        {
            return new TestRunSettings(xml);
        }

        [TestMethod]
        public void GetTSQLtSetting_ValidSetting_ReturnsValue()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <DatabaseConnectionString>Server=.;Database=TestDB;Integrated Security=true</DatabaseConnectionString>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSetting(runSettings, "DatabaseConnectionString");

            // Assert
            Assert.AreEqual("Server=.;Database=TestDB;Integrated Security=true", result);
        }

        [TestMethod]
        public void GetTSQLtSetting_MissingSetting_ReturnsNull()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <DatabaseConnectionString>Server=.;Database=TestDB;Integrated Security=true</DatabaseConnectionString>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSetting(runSettings, "NonExistentSetting");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetTSQLtSetting_NoTSQLtSection_ReturnsNull()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSetting(runSettings, "DatabaseConnectionString");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetTSQLtSetting_NullRunSettings_ReturnsNull()
        {
            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSetting(null, "DatabaseConnectionString");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetTSQLtSettingBool_ValidTrue_ReturnsTrue()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <CaptureTestOutput>true</CaptureTestOutput>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettingBool(runSettings, "CaptureTestOutput", false);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GetTSQLtSettingBool_ValidFalse_ReturnsFalse()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <CaptureTestOutput>false</CaptureTestOutput>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettingBool(runSettings, "CaptureTestOutput", true);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetTSQLtSettingBool_MissingSetting_ReturnsDefault()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettingBool(runSettings, "CaptureTestOutput", true);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GetTSQLtSettings_MultipleValues_ReturnsAll()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <TestFolder>C:\Project\Database</TestFolder>
    <TestFolder>C:\Project\Tests</TestFolder>
    <TestFolder>C:\Project\Integration</TestFolder>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettings(runSettings, "TestFolder");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(@"C:\Project\Database"));
            Assert.IsTrue(result.Contains(@"C:\Project\Tests"));
            Assert.IsTrue(result.Contains(@"C:\Project\Integration"));
        }

        [TestMethod]
        public void GetTSQLtSettings_NoValues_ReturnsEmptyList()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettings(runSettings, "TestFolder");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetTSQLtSettings_NullRunSettings_ReturnsEmptyList()
        {
            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSettings(null, "TestFolder");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetTSQLtSetting_EmptyValue_ReturnsEmptyString()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <tSQLt>
    <DatabaseConnectionString></DatabaseConnectionString>
  </tSQLt>
</RunSettings>";
            var runSettings = CreateRunSettings(xml);

            // Act
            var result = tSQLt.TestAdapter.RunSettingsHelper.GetTSQLtSetting(runSettings, "DatabaseConnectionString");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }
    }
}
