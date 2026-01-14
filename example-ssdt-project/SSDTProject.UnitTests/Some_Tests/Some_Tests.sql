CREATE SCHEMA [Some_Tests];
GO

EXECUTE sp_addextendedproperty
        @name = N'tSQLt.TestClass'
, @value = 1
, @level0type = N'SCHEMA'
, @level0name = N'Some_Tests';

GO


