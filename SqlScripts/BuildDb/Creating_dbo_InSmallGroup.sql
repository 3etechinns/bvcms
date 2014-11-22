

CREATE FUNCTION [dbo].[InSmallGroup] ( @oid INT, @pid INT, @sg VARCHAR(100) )
RETURNS VARCHAR(100)
AS
BEGIN
	DECLARE @ret VARCHAR(100)

	SELECT @ret = mt.Name FROM dbo.OrgMemMemTags ommt
	JOIN dbo.MemberTags mt ON mt.Id = ommt.MemberTagId
	WHERE mt.Name = @sg
	AND mt.OrgId = @oid
	AND ommt.PeopleId = @pid

	RETURN @ret

END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
