using Microsoft.VisualStudio.TestTools.UnitTesting;
using tSQLt.TestAdapter.Client;
using tSQLt.TestAdapter.Client.Parsers;

namespace tSQlt.TestAdapter.UnitTests.Client
{
    [TestClass]
    public class XmlParserTests
    {
        [TestMethod]
        public void Get_ValidXml_ReturnsTestSuites()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""MyTestClass"" tests=""2"" failures=""0"" errors=""0"">
                    <testcase classname=""MyTestClass"" name=""test Should Pass"" />
                    <testcase classname=""MyTestClass"" name=""test Another Pass"" />
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Suites.Count);
            Assert.AreEqual("MyTestClass", result.Suites[0].Name);
            Assert.AreEqual(2, result.Suites[0].TestCount);
            Assert.AreEqual(0, result.Suites[0].FailureCount);
            Assert.AreEqual(0, result.Suites[0].ErrorCount);
            Assert.AreEqual(2, result.Suites[0].Tests.Count);
        }

        [TestMethod]
        public void Get_XmlWithFailures_ParsesFailureMessages()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""MyTestClass"" tests=""1"" failures=""1"" errors=""0"">
                    <testcase classname=""MyTestClass"" name=""test Should Fail"">
                        <failure message=""Expected value does not match actual value"" />
                    </testcase>
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Suites[0].FailureCount);
            Assert.AreEqual(1, result.Suites[0].Tests.Count);
            Assert.IsNotNull(result.Suites[0].Tests[0].Failure);
            Assert.AreEqual("Expected value does not match actual value", result.Suites[0].Tests[0].Failure.Message);
        }

        [TestMethod]
        public void Get_XmlWithErrors_ParsesErrorMessages()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""MyTestClass"" tests=""1"" failures=""0"" errors=""1"">
                    <testcase classname=""MyTestClass"" name=""test Should Error"">
                        <error message=""Invalid object name 'NonExistentTable'"" />
                    </testcase>
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Suites[0].ErrorCount);
            Assert.AreEqual(1, result.Suites[0].Tests.Count);
            Assert.IsNotNull(result.Suites[0].Tests[0].Error);
            Assert.AreEqual("Invalid object name 'NonExistentTable'", result.Suites[0].Tests[0].Error.Message);
        }

        [TestMethod]
        public void Get_MultipleTestSuites_ParsesAllSuites()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""Suite1"" tests=""2"" failures=""0"" errors=""0"">
                    <testcase classname=""Suite1"" name=""test One"" />
                    <testcase classname=""Suite1"" name=""test Two"" />
                </testsuite>
                <testsuite name=""Suite2"" tests=""1"" failures=""0"" errors=""0"">
                    <testcase classname=""Suite2"" name=""test Three"" />
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Suites.Count);
            Assert.AreEqual("Suite1", result.Suites[0].Name);
            Assert.AreEqual("Suite2", result.Suites[1].Name);
            Assert.AreEqual(2, result.Suites[0].Tests.Count);
            Assert.AreEqual(1, result.Suites[1].Tests.Count);
        }

        [TestMethod]
        public void Get_InvalidXml_ReturnsTestSuitesWithErrorMessage()
        {
            // Arrange
            var invalidXml = "<not-valid-xml>";

            // Act
            var result = XmlParser.Get(invalidXml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Suites.Count);
            // Should contain error information from the exception
        }

        [TestMethod]
        public void Get_EmptyTestSuite_ParsesCorrectly()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""EmptyTestClass"" tests=""0"" failures=""0"" errors=""0"">
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Suites.Count);
            Assert.AreEqual("EmptyTestClass", result.Suites[0].Name);
            Assert.AreEqual(0, result.Suites[0].TestCount);
        }

        [TestMethod]
        public void Get_SpecialCharactersInMessages_HandlesCorrectly()
        {
            // Arrange
            var xml = @"<testsuites>
                <testsuite name=""MyTestClass"" tests=""1"" failures=""1"" errors=""0"">
                    <testcase classname=""MyTestClass"" name=""test Special Chars"">
                        <failure message=""Expected &lt;value&gt; but got &quot;something&quot; &amp; more"" />
                    </testcase>
                </testsuite>
            </testsuites>";

            // Act
            var result = XmlParser.Get(xml);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Suites[0].Tests[0].Failure);
            Assert.IsTrue(result.Suites[0].Tests[0].Failure.Message.Contains("<value>"));
        }
    }
}
