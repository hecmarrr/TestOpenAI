CREATE OR ALTER PROCEDURE dbo.sp_Sync_HasDeliveredFingerprint
    @TableName NVARCHAR(256),
    @Fingerprint CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1)
    FROM dbo.SyncDeliveryLog
    WHERE TableName = @TableName
      AND Fingerprint = @Fingerprint
      AND DeliveryStatus = 'Delivered';
END;
