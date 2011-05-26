SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
CREATE FUNCTION [dbo].[GetPeopleIdFromIndividualNumber](@indnum int)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @id int

	-- Add the T-SQL statements to compute the return value here
	SELECT @id = PeopleId 
	FROM dbo.PeopleExtra
	WHERE Field = 'IndividualNumber' 
	AND IntValue = @indnum

	-- Return the result of the function
	RETURN @id

END
GO
