using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using tSQLt.TestAdapter.Client;

namespace tSQlt.TestAdapter.UnitTests.Client
{
    [TestClass]
    public class TestSuitesTests
    {
        [TestMethod]
        public void Passed_AllSuitesPass_ReturnsTrue()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 },
                    new TestSuite { TestCount = 1, FailureCount = 0, ErrorCount = 0 }
                }
            };

            // Act
            var result = testSuites.Passed();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Passed_OneSuiteFails_ReturnsFalse()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 },
                    new TestSuite { TestCount = 1, FailureCount = 1, ErrorCount = 0 }
                }
            };

            // Act
            var result = testSuites.Passed();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Passed_OneSuiteHasError_ReturnsFalse()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 },
                    new TestSuite { TestCount = 1, FailureCount = 0, ErrorCount = 1 }
                }
            };

            // Act
            var result = testSuites.Passed();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestCount_MultipleSuites_ReturnsSumOfTestCounts()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 5, FailureCount = 0, ErrorCount = 0 },
                    new TestSuite { TestCount = 3, FailureCount = 0, ErrorCount = 0 },
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 }
                }
            };

            // Act
            var result = testSuites.TestCount();

            // Assert
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void FailureCount_MultipleSuites_ReturnsSumOfFailures()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 5, FailureCount = 2, ErrorCount = 0 },
                    new TestSuite { TestCount = 3, FailureCount = 1, ErrorCount = 0 },
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 }
                }
            };

            // Act
            var result = testSuites.FailureCount();

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void ErrorCount_MultipleSuites_ReturnsSumOfErrors()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite { TestCount = 5, FailureCount = 0, ErrorCount = 1 },
                    new TestSuite { TestCount = 3, FailureCount = 0, ErrorCount = 2 },
                    new TestSuite { TestCount = 2, FailureCount = 0, ErrorCount = 0 }
                }
            };

            // Act
            var result = testSuites.ErrorCount();

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void FailureMessages_SuitesWithFailures_ConcatenatesMessages()
        {
            // Arrange
            var testSuites = new TestSuites
            {
                Suites = new List<TestSuite>
                {
                    new TestSuite
                    {
                        TestCount = 2,
                        FailureCount = 1,
                        ErrorCount = 0,
                        Tests = new List<Test>
                        {
                            new Test
                            {
                                ClassName = "MyTestClass",
                                Name = "test Should Pass",
                                Failure = null
                            },
                            new Test
                            {
                                ClassName = "MyTestClass",
                                Name = "test Should Fail",
                                Failure = new Failure { Message = "Assertion failed" }
                            }
                        }
                    }
                }
            };

            // Act
            var result = testSuites.FailureMessages();

            // Assert
            Assert.IsTrue(result.Contains("MyTestClass.test Should Fail"));
            Assert.IsTrue(result.Contains("Assertion failed"));
        }

        [TestMethod]
        public void WasSuccess_NoTests_ReturnsFalse()
        {
            // Arrange
            var testSuite = new TestSuite { TestCount = 0, FailureCount = 0, ErrorCount = 0 };

            // Act
            var result = testSuite.WasSuccess();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void WasSuccess_TestsWithNoFailuresOrErrors_ReturnsTrue()
        {
            // Arrange
            var testSuite = new TestSuite { TestCount = 3, FailureCount = 0, ErrorCount = 0 };

            // Act
            var result = testSuite.WasSuccess();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WasSuccess_TestsWithFailures_ReturnsFalse()
        {
            // Arrange
            var testSuite = new TestSuite { TestCount = 3, FailureCount = 1, ErrorCount = 0 };

            // Act
            var result = testSuite.WasSuccess();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void WasSuccess_TestsWithErrors_ReturnsFalse()
        {
            // Arrange
            var testSuite = new TestSuite { TestCount = 3, FailureCount = 0, ErrorCount = 1 };

            // Act
            var result = testSuite.WasSuccess();

            // Assert
            Assert.IsFalse(result);
        }
    }
}
