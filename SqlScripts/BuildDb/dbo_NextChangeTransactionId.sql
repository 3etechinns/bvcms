CREATE FUNCTION [dbo].[NextChangeTransactionId]
(
  @pid int
 ,@oid int
 ,@tid int
 ,@typeid int
)
RETURNS int
AS
	BEGIN
	  DECLARE @rtid int 
		  select top 1 @rtid = TransactionId
			from dbo.EnrollmentTransaction
		   where TransactionTypeId >= 3
		     and @typeid <= 3
			 and PeopleId = @pid
			 and OrganizationId = @oid
			 and TransactionId > @tid
	   order by TransactionId
	RETURN @rtid
	END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
