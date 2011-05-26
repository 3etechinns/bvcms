SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

CREATE VIEW [dbo].Sprocs
AS
SELECT ROUTINE_TYPE type, ROUTINE_NAME Name, ROUTINE_DEFINITION Code
    FROM INFORMATION_SCHEMA.ROUTINES 
    WHERE ROUTINE_TYPE IN ('FUNCTION','PROCEDURE')
    AND SPECIFIC_NAME NOT LIKE 'sp_%'
    AND SPECIFIC_NAME NOT LIKE 'fn_%'

GO
