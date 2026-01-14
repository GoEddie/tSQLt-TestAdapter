CREATE PROCEDURE [Some_Tests].[test this is a test that fails]
AS
	select * from sys.objects;
	exec tSQLt.Fail 'uh oh';
RETURN 0

GO
CREATE PROCEDURE [Some_Tests].[test this is a test that fails 2]
AS
	select * from sys.objects;
	exec tSQLt.Fail 'uh oh';
RETURN 0
