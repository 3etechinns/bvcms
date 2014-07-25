CREATE FUNCTION [dbo].[FindPerson](@first nvarchar(25), @last nvarchar(50), @dob DATETIME, @email nvarchar(60), @phone nvarchar(15))
RETURNS @t TABLE ( PeopleId INT)
AS
BEGIN
--DECLARE @t TABLE ( PeopleId INT)
--DECLARE @first nvarchar(25) = 'Beth', 
--		@last nvarchar(50) = 'Marcus', 
--		@dob DATETIME = '1/29/2013', 
--		@email nvarchar(60) = 'b@b.com', 
--		@phone nvarchar(15) = '9017581862'
		
	DECLARE @fname nvarchar(50) = REPLACE(@first,' ', '')
	SET @dob = CASE WHEN @dob = '' THEN NULL ELSE @dob END
	
	DECLARE @m INT = DATEPART(m, @dob)
	DECLARE @d INT = DATEPART(d, @dob)
	DECLARE @y INT = DATEPART(yy, @dob)
	
	DECLARE @mm TABLE ( PeopleId INT, Matches INT )
	SET @phone = dbo.GetDigits(@phone)
	
	INSERT INTO @mm
	SELECT PeopleId, -- col 1
	(
		CASE WHEN (ISNULL(@email, '') = '' AND ISNULL(@phone, '') = '' AND @dob IS NULL)
		OR (p.EmailAddress = @email AND LEN(@email) > 0)
		OR (p.EmailAddress2 = @email AND LEN(@email) > 0) 
		THEN 1 ELSE 0 END 
		+
		CASE WHEN (f.HomePhone = @phone AND LEN(@phone) > 0)
		OR (CellPhone = @phone AND LEN(@phone) > 0)
		THEN 1 ELSE 0 END 
		+
		CASE WHEN (BirthDay = @d AND BirthMonth = @m AND ABS(BirthYear - @y) <= 1)
		OR (BirthDay = @d AND BirthMonth = @m AND BirthYear IS NULL)
		THEN 1 ELSE 0 END
	) matches -- col 2
	FROM dbo.People p
	JOIN dbo.Families f ON p.FamilyId = f.FamilyId
	WHERE
	(
		   @fname = FirstName
		OR @fname = NickName
		OR FirstName2 LIKE (@fname + '%')
		OR @fname LIKE (FirstName + '%')
	)
	AND (@last = LastName OR @last = MaidenName OR @last = MiddleName)
	
	
	--SELECT p.PeopleId, Matches, Name, BirthMonth, BirthDay, BirthYear, EmailAddress, CellPhone FROM @mm
	--JOIN dbo.People p ON [@mm].PeopleId = p.PeopleId
	
	INSERT INTO @t
	SELECT PeopleId FROM @mm m1
	WHERE m1.Matches = (SELECT MAX(Matches) FROM @mm) AND m1.Matches > 0
	ORDER BY Matches DESC
	
	--SELECT * FROM @t
	
	RETURN
END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
