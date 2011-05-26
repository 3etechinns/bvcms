SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

CREATE PROCEDURE [dbo].[UpdateMeetingCounters](@mid INT)
AS
BEGIN
	DECLARE @gf BIT, @numPresent INT, @numNewVisit INT, @numMembers INT, @numVstMembers INT, @numRepeatVst INT, @numOutTowners INT, @numOtherAttends INT
	SELECT @gf = GroupMeetingFlag FROM dbo.Meetings WHERE MeetingId = @mid
	
	SELECT @numNewVisit = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId = 60
	SELECT @numRepeatVst = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId = 50
	SELECT @numVstMembers = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId = 40
	SELECT @numOutTowners = COUNT(*) FROM dbo.Attend a WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId IN (50,60) 
		AND EXISTS(SELECT NULL FROM dbo.People WHERE a.PeopleId = PeopleId AND PrimaryResCode NOT IN (10, 20))
	SELECT @numOtherAttends = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceTypeId = 80
	
	IF @gf = 0
	BEGIN
		SELECT @numPresent = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceFlag = 1
		SELECT @numMembers = COUNT(*) FROM dbo.Attend WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId IN (10,20,30)
		SELECT @numOutTowners = COUNT(*) FROM dbo.Attend a WHERE MeetingId = @mid AND AttendanceFlag = 1 AND AttendanceTypeId IN (50,60) AND EXISTS(SELECT NULL FROM dbo.People WHERE a.PeopleId = PeopleId AND PrimaryResCode NOT IN (10, 20))
		UPDATE dbo.Meetings SET
			NumMembers = @numMembers,
			NumPresent = @numPresent,
			NumNewVisit = @numNewVisit,
			NumVstMembers = @numVstMembers,
			NumRepeatVst = @numRepeatVst,
			NumOutTown = @numOutTowners,
			NumOtherAttends = @numOtherAttends
			WHERE MeetingId = @mid
	END
	ELSE
	BEGIN		
		UPDATE dbo.Meetings SET
			NumNewVisit = @numNewVisit,
			NumVstMembers = @numVstMembers,
			NumRepeatVst = @numRepeatVst,
			NumOutTown = @numOutTowners,
			NumOtherAttends = @numOtherAttends
			WHERE MeetingId = @mid
	END
END
GO
