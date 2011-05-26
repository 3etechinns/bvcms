SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

CREATE PROCEDURE [dbo].[DisableForeignKeys]
    @disable BIT = 1
AS
    DECLARE
        @sql VARCHAR(500),
        @tableName VARCHAR(128),
        @foreignKeyName VARCHAR(128),
		@schema varchar(50)

    -- A list of all foreign keys and table names
    DECLARE foreignKeyCursor CURSOR
    FOR SELECT
        ref.constraint_name AS FK_Name,
        fk.table_name AS FK_Table,
		ref.Constraint_schema as FK_Schema
    FROM
        INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS ref
        INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS fk 
    ON ref.constraint_name = fk.constraint_name
    ORDER BY
        fk.table_name,
        ref.constraint_name 

    OPEN foreignKeyCursor

    FETCH NEXT FROM foreignKeyCursor 
    INTO @foreignKeyName, @tableName, @schema

    WHILE ( @@FETCH_STATUS = 0 )
        BEGIN
            IF @disable = 1
                SET @sql = 'ALTER TABLE ' + @schema + '.[' 
                    + @tableName + '] NOCHECK CONSTRAINT ['
                    + @foreignKeyName + ']'
            ELSE
                SET @sql = 'ALTER TABLE ' + @schema + '.[' 
                    + @tableName + '] CHECK CONSTRAINT ['
                    + @foreignKeyName + ']'

        PRINT 'Executing Statement - ' + @sql

        EXECUTE(@sql)
        FETCH NEXT FROM foreignKeyCursor 
        INTO @foreignKeyName, @tableName, @schema
    END

    CLOSE foreignKeyCursor
    DEALLOCATE foreignKeyCursor


GO
