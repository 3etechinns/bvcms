CREATE FUNCTION [dbo].[RegisterLinksFromMaster](@master INT)
RETURNS TABLE 
AS
RETURN 
(
	SELECT 
		o.OrganizationId
		,NULLIF(dbo.RegexMatch(o.RegSetting, '(?<=^Title:\s)(.*)$'), '') AS Title
		,o.OrganizationName
		,o.[Description]
		,o.AppCategory
		,o.PublicSortOrder
		,o.UseRegisterLink2
		,RegisterLinkHeader = e.Data
		
	FROM dbo.Organizations o
	LEFT JOIN dbo.OrganizationExtra e ON e.OrganizationId = o.OrganizationId AND e.Field = 'RegisterLinkHeader'
	WHERE o.ParentOrgId = @master
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
