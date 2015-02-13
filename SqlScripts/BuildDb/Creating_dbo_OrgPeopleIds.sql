CREATE FUNCTION [dbo].[OrgPeopleIds](
	 @oid INT
	,@grouptype VARCHAR(20)
	,@first VARCHAR(30)
	,@last VARCHAR(30)
	,@sgfilter VARCHAR(300)
	,@showhidden BIT
	,@currtag NVARCHAR(300)
	,@currtagowner INT
	,@filterchecked BIT
	,@filtertag BIT
	,@ministryinfo BIT
	,@userpeopleid INT
) RETURNS TABLE
AS
RETURN
(
	SELECT PeopleId
	FROM dbo.OrgPeople(@oid, @grouptype, @first, @last, @sgfilter, 
		@showhidden, @currtag, @currtagowner, @filterchecked, @filtertag, @ministryinfo, @userpeopleid)
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
