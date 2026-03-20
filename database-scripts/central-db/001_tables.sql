IF OBJECT_ID('dbo.IntegrationInbox', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationInbox
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        SourceTable NVARCHAR(256) NOT NULL,
        SourceQuery NVARCHAR(MAX) NOT NULL,
        PrimaryKeyValue NVARCHAR(256) NOT NULL,
        WatermarkUtc DATETIME2 NOT NULL,
        Fingerprint CHAR(64) NOT NULL,
        PayloadFormat NVARCHAR(16) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        ReceivedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT UX_IntegrationInbox_Fingerprint UNIQUE (Fingerprint)
    );
END;
