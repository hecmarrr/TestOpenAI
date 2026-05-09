CREATE OR ALTER PROCEDURE dbo.sp_Sync_GetCheckpoint
    @TableName NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT LastWatermarkUtc, LastPrimaryKeyValue
    FROM dbo.SyncCheckpoint
    WHERE TableName = @TableName;
END;
