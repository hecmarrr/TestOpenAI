CREATE OR ALTER PROCEDURE dbo.sp_Sync_UpsertDeliveryState
    @TableName NVARCHAR(256),
    @PrimaryKeyValue NVARCHAR(256),
    @WatermarkUtc DATETIME2,
    @Fingerprint CHAR(64),
    @Payload NVARCHAR(MAX),
    @DeliveryStatus NVARCHAR(32),
    @LastError NVARCHAR(MAX) = NULL,
    @LastWatermarkUtc DATETIME2,
    @LastPrimaryKeyValue NVARCHAR(256),
    @UpdatedAtUtc DATETIME2,
    @UpdateCheckpoint BIT
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.SyncDeliveryLog AS target
    USING (SELECT @TableName AS TableName, @Fingerprint AS Fingerprint) AS source
    ON target.TableName = source.TableName
       AND target.Fingerprint = source.Fingerprint
    WHEN MATCHED THEN
        UPDATE SET
            PrimaryKeyValue = @PrimaryKeyValue,
            WatermarkUtc = @WatermarkUtc,
            Payload = @Payload,
            DeliveryStatus = @DeliveryStatus,
            LastError = @LastError,
            UpdatedAtUtc = @UpdatedAtUtc
    WHEN NOT MATCHED THEN
        INSERT (TableName, PrimaryKeyValue, WatermarkUtc, Fingerprint, Payload, DeliveryStatus, LastError, UpdatedAtUtc)
        VALUES (@TableName, @PrimaryKeyValue, @WatermarkUtc, @Fingerprint, @Payload, @DeliveryStatus, @LastError, @UpdatedAtUtc);

    IF (@UpdateCheckpoint = 1)
    BEGIN
        MERGE dbo.SyncCheckpoint AS target
        USING (SELECT @TableName AS TableName) AS source
        ON target.TableName = source.TableName
        WHEN MATCHED THEN
            UPDATE SET
                LastWatermarkUtc = @LastWatermarkUtc,
                LastPrimaryKeyValue = @LastPrimaryKeyValue,
                UpdatedAtUtc = @UpdatedAtUtc
        WHEN NOT MATCHED THEN
            INSERT (TableName, LastWatermarkUtc, LastPrimaryKeyValue, UpdatedAtUtc)
            VALUES (@TableName, @LastWatermarkUtc, @LastPrimaryKeyValue, @UpdatedAtUtc);
    END;
END;
