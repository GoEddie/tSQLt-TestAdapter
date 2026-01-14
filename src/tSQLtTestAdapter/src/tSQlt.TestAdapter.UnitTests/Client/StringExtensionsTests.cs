using Microsoft.VisualStudio.TestTools.UnitTesting;
using tSQLt.TestAdapter.Client;

namespace tSQlt.TestAdapter.UnitTests.Client
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void UnQuote_BracketedString_RemovesBrackets()
        {
            // Arrange
            var input = "[MySchema]";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("MySchema", result);
        }


        [TestMethod]
        public void UnQuote_UnbracketedString_ReturnsUnchanged()
        {
            // Arrange
            var input = "MySchema";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("MySchema", result);
        }

        [TestMethod]
        public void UnQuote_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void UnQuote_OnlyBrackets_ReturnsEmpty()
        {
            // Arrange
            var input = "[]";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void UnQuote_SingleOpeningBracket_RemovesIt()
        {
            // Arrange
            var input = "[MySchema";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("MySchema", result);
        }

        [TestMethod]
        public void UnQuote_SingleClosingBracket_RemovesIt()
        {
            // Arrange
            var input = "MySchema]";

            // Act
            var result = input.UnQuote();

            // Assert
            Assert.AreEqual("MySchema", result);
        }
    }
}
