SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

CREATE  FUNCTION [dbo].[SundayDates](@dt1 datetime, @dt2 datetime)
RETURNS @dates TABLE
(   
	dt datetime
)
AS
BEGIN
	DECLARE @dt DATETIME = dbo.SundayForDate(@dt1)
	WHILE (@dt <= @dt2)
	BEGIN
		IF @dt >= @dt1
			INSERT INTO @dates VALUES (@dt)
		SET @dt = DATEADD(dd, 7, @dt)
	END
	RETURN
END
GO
