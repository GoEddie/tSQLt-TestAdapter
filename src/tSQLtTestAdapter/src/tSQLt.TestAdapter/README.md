# tSQLt Test Adapter for Visual Studio

A Visual Studio Test Adapter that enables discovering and running [tSQLt](https://tsqlt.org/) database unit tests directly from Visual Studio Test Explorer.

## Features

- **Automatic Test Discovery** - Discovers tSQLt tests from DACPAC files
- **Test Explorer Integration** - Run tests directly from Visual Studio Test Explorer
- **Source Navigation** - Click on tests to navigate to the SQL source file
- **Real-time Results** - See test results, failures, and error messages in Test Explorer
- **SQL Server Integration** - Runs tests against your SQL Server database using tSQLt

## Installation

Install via NuGet Package Manager:

```powershell
Install-Package tSQLt.TestAdapter
```


## Configuration

Create a `.runsettings` file in your solution with the following configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <tSQLt>
    <!-- Required: Connection string to your test database -->
    <DatabaseConnectionString>Server=.;Database=MyTestDatabase;Integrated Security=true</DatabaseConnectionString>

    <!-- Optional: Root folder(s) containing SQL source files -->
    <!-- You can specify multiple TestFolder elements to search multiple directories -->
    <TestFolder>C:\MyProject\Database</TestFolder>
    <TestFolder>C:\MyProject\Tests</TestFolder>

    <!-- Optional: Launch debugger when tests are discovered -->
    <LaunchDebugger>false</LaunchDebugger>
  </tSQLt>
</RunSettings>
```

Configure Visual Studio to use this file:
1. Test → Configure Run Settings → Select Solution Wide runsettings File
2. Browse to your `.runsettings` file

## How It Works

1. **Test Discovery**: The adapter scans your DACPAC for schemas marked with the `tSQLt.TestClass` extended property
2. **Test Identification**: Within those schemas, it finds stored procedures whose names start with "test"
3. **Source Mapping**: Parses your SQL source files to map tests to their file locations
4. **Test Execution**: Uses tSQLt.Client.Net to execute tests against your SQL Server database
5. **Results Reporting**: Reports pass/fail results back to Visual Studio Test Explorer

## Example Test Structure

```sql
-- Create a test class (schema with extended property)
EXEC tSQLt.NewTestClass 'MyTestClass';
GO

-- Create a test procedure
CREATE PROCEDURE MyTestClass.testShouldAddNumbersCorrectly
AS
BEGIN
    -- Arrange
    DECLARE @result INT;

    -- Act
    SET @result = 2 + 2;

    -- Assert
    EXEC tSQLt.AssertEquals 4, @result;
END;
GO
```

## Requirements

- Visual Studio 2017 or later
- SQL Server with tSQLt installed
- .NET Framework 4.7.2 or later

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/GoEddie/tSQLt-TestAdapter).

## License

MIT License - see LICENSE file for details
