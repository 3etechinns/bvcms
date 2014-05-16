-- ================================================
-- Template generated from Template Explorer using:
-- Create Inline Function (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the function.
-- ================================================
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================



CREATE FUNCTION dbo.SplitInts ( @List VARCHAR(MAX) )
RETURNS TABLE
AS
    RETURN
    (
        SELECT DISTINCT
            [Value] = CONVERT(INT, LTRIM(RTRIM(CONVERT(
                VARCHAR(12),
                SUBSTRING(@List, Number,
                CHARINDEX(',', @List + ',', Number) - Number)))))
        FROM
            dbo.Numbers
        WHERE
            Number <= CONVERT(INT, LEN(@List))
            AND SUBSTRING(',' + @List, Number, 1) = ','
    );
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
