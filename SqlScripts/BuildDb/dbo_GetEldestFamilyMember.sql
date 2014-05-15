CREATE FUNCTION [dbo].[GetEldestFamilyMember]( @fid int )
RETURNS int
AS
BEGIN
    DECLARE @Result int

    select @Result = PeopleId
      from dbo.People
     where FamilyId = @fid
       and dbo.Birthday(PeopleId) = (select min(dbo.Birthday(PeopleId))
                    from dbo.People
                   where FamilyId = @fid)
                   
    IF @Result IS NULL
		SELECT TOP 1 @Result = PeopleId FROM dbo.People WHERE FamilyId = @fid
     
	RETURN @Result
END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
