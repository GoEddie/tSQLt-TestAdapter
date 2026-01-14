# tSQLt.TestAdapter.UnitTests

Unit tests for the tSQLt Visual Studio Test Adapter.

## Overview

This project contains comprehensive unit tests for the tSQLt.TestAdapter library, ensuring reliability and correctness of the test discovery and execution infrastructure.

## Test Coverage

### Client Library Tests

#### XmlParserTests
Tests for the XML deserialization of tSQLt test results:
- Valid XML parsing
- Handling of failures and errors
- Multiple test suites
- Invalid XML handling
- Edge cases (empty suites, special characters)

#### StringExtensionsTests
Tests for the string extension methods used in SQL identifier quoting:
- Bracket removal (`UnQuote`)
- Edge cases (empty strings, nested brackets)

#### TestSuitesTests
Tests for the test results aggregation classes:
- `TestSuites.Passed()` - Overall pass/fail determination
- `TestCount()`, `FailureCount()`, `ErrorCount()` - Aggregation methods
- `FailureMessages()` - Concatenation of error messages
- `TestSuite.WasSuccess()` - Individual suite success determination

#### ResultSetFormatterTests
Tests for the result set formatting functionality:
- Table formatting with proper alignment
- Column width calculation
- Header and separator rendering
- Edge cases (empty results, missing values)

#### tSQLtTestRunnerTests
Tests for the main test runner using test SQL gateway:
- All tests execution (`RunAll`) - No validation required
- Constructor tests

**Note:** Tests for `Run()`, `RunClass()`, and `RunWithCapture()` are not included in unit tests because they require validation logic that depends on a real `SqlDataReader`, which cannot be easily created without a mocking framework or a real database connection. These methods should be tested through integration tests with a real SQL Server database containing tSQLt test structures.

### Helper Tests

#### RunSettingsHelperTests
Tests for the runsettings configuration parser:
- Single value settings (`GetTSQLtSetting`)
- Boolean settings with defaults (`GetTSQLtSettingBool`)
- Multiple value settings (`GetTSQLtSettings`)
- Missing settings and default values
- Null input handling

## Dependencies

- **MSTest** (2.2.10) - Test framework
- **tSQLt.TestAdapter** - Project under test

## Running Tests

### Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Click "Run All" to execute all tests

### Command Line
```powershell
dotnet test tSQlt.TestAdapter.UnitTests.csproj
```

Or using vstest.console.exe:
```powershell
vstest.console.exe bin\Debug\tSQlt.TestAdapter.UnitTests.dll
```

## Test Structure

Tests follow the AAA (Arrange-Act-Assert) pattern:

```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    var input = "test data";

    // Act - Execute the method under test
    var result = SystemUnderTest.Method(input);

    // Assert - Verify the expected outcome
    Assert.AreEqual(expected, result);
}
```

## Test Implementation Strategy

Database interactions are handled using lightweight test implementations (test doubles) to ensure tests run quickly without requiring a SQL Server instance:

```csharp
var gateway = new TestSqlServerGateway();
gateway.ExpectedXmlResult = expectedXml;

var runner = new tSQLtTestRunner(gateway);
```

The test project includes simple implementations of:
- `TestRunSettings` - Implements `IRunSettings` for testing configuration parsing
- `TestSqlServerGateway` - Implements `ISqlServerGateway` for testing database interactions (validation methods throw `NotImplementedException`)

This approach avoids complex mocking framework dependencies while keeping tests fast and maintainable.

### Test Scope

These unit tests focus on:
- **Pure logic** - XML parsing, string manipulation, configuration reading, result aggregation
- **Constructor behavior** - Object initialization
- **Non-validation paths** - `RunAll()` method that doesn't require test existence validation

**Not covered** by unit tests (requires integration tests):
- Test validation logic (requires real `SqlDataReader` from database)
- `Run()` and `RunClass()` methods (include validation)
- `RunWithCapture()` method (includes validation)

For complete test coverage, supplement these unit tests with integration tests that use a real SQL Server database with tSQLt installed.

## Contributing

When adding new functionality to tSQLt.TestAdapter:
1. Write corresponding unit tests
2. Follow the existing naming conventions (`MethodName_Scenario_ExpectedBehavior`)
3. Maintain test independence (no shared state between tests)
4. Use test implementations (test doubles) for external dependencies
5. Aim for high code coverage of critical paths
6. Keep tests fast by avoiding real database connections
