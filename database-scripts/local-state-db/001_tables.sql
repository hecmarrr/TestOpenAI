IF OBJECT_ID('dbo.SyncDeliveryLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncDeliveryLog
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(256) NOT NULL,
        PrimaryKeyValue NVARCHAR(256) NOT NULL,
        WatermarkUtc DATETIME2 NOT NULL,
        Fingerprint CHAR(64) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        DeliveryStatus NVARCHAR(32) NOT NULL,
        LastError NVARCHAR(MAX) NULL,
        UpdatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT UX_SyncDeliveryLog UNIQUE (TableName, Fingerprint)
    );

    CREATE INDEX IX_SyncDeliveryLog_Table_Watermark
        ON dbo.SyncDeliveryLog (TableName, WatermarkUtc DESC, PrimaryKeyValue DESC);
END;

IF OBJECT_ID('dbo.SyncCheckpoint', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncCheckpoint
    (
        TableName NVARCHAR(256) NOT NULL PRIMARY KEY,
        LastWatermarkUtc DATETIME2 NULL,
        LastPrimaryKeyValue NVARCHAR(256) NULL,
        UpdatedAtUtc DATETIME2 NOT NULL
    );
END;
