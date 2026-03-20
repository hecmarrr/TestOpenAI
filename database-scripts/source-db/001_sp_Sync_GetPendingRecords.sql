CREATE OR ALTER PROCEDURE dbo.sp_Sync_GetPendingRecords
    @TableName NVARCHAR(256),
    @PrimaryKeyColumn NVARCHAR(256),
    @WatermarkColumn NVARCHAR(256),
    @BatchSize INT,
    @LastWatermarkUtc DATETIME2 = NULL,
    @LastPrimaryKeyValue NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SchemaName NVARCHAR(128) = ISNULL(PARSENAME(@TableName, 2), 'dbo');
    DECLARE @ObjectName NVARCHAR(128) = PARSENAME(@TableName, 1);

    DECLARE @Sql NVARCHAR(MAX) = N'
        SELECT TOP (@BatchSize) *
        FROM ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@ObjectName) + N'
        WHERE (
            @LastWatermarkUtc IS NULL
            OR ' + QUOTENAME(@WatermarkColumn) + N' > @LastWatermarkUtc
            OR (
                ' + QUOTENAME(@WatermarkColumn) + N' = @LastWatermarkUtc
                AND CONVERT(NVARCHAR(256), ' + QUOTENAME(@PrimaryKeyColumn) + N') > @LastPrimaryKeyValue
            )
        )
        ORDER BY ' + QUOTENAME(@WatermarkColumn) + N', ' + QUOTENAME(@PrimaryKeyColumn) + N';';

    EXEC sp_executesql
        @Sql,
        N'@BatchSize INT, @LastWatermarkUtc DATETIME2, @LastPrimaryKeyValue NVARCHAR(256)',
        @BatchSize = @BatchSize,
        @LastWatermarkUtc = @LastWatermarkUtc,
        @LastPrimaryKeyValue = @LastPrimaryKeyValue;
END;
