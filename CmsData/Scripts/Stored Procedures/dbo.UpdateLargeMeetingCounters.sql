SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE UpdateLargeMeetingCounters
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
		DECLARE cur CURSOR FOR SELECT MeetingId FROM dbo.Meetings WHERE NumPresent > 4000
		OPEN cur
		DECLARE @mid int
		FETCH NEXT FROM cur INTO @mid
		WHILE @@FETCH_STATUS = 0
		BEGIN
			EXECUTE dbo.UpdateMeetingCounters @mid
			FETCH NEXT FROM cur INTO @mid
		END
		CLOSE cur
		DEALLOCATE cur
END
GO
