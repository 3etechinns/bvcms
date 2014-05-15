
CREATE VIEW [dbo].[PreviousMemberCounts]
AS
SELECT et.OrganizationId, COUNT(*) prevcount
FROM dbo.EnrollmentTransaction et 
WHERE TransactionStatus = 0
AND TransactionTypeId > 3
AND TransactionDate = (SELECT MAX(TransactionDate) FROM dbo.EnrollmentTransaction WHERE PeopleId = et.PeopleId AND OrganizationId = et.OrganizationId AND TransactionTypeId > 3) 
AND NOT EXISTS(SELECT NULL FROM dbo.OrganizationMembers WHERE PeopleId = et.PeopleId AND OrganizationId = et.OrganizationId)
GROUP BY et.OrganizationId
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
