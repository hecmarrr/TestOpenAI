CREATE OR ALTER PROCEDURE dbo.sp_Sync_StoreIntegrationInbox
    @SourceTable NVARCHAR(256),
    @SourceQuery NVARCHAR(MAX),
    @PrimaryKeyValue NVARCHAR(256),
    @WatermarkUtc DATETIME2,
    @Fingerprint CHAR(64),
    @PayloadFormat NVARCHAR(16),
    @Payload NVARCHAR(MAX),
    @ReceivedAtUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId BIGINT;

    SELECT TOP 1 @ExistingId = Id
    FROM dbo.IntegrationInbox
    WHERE Fingerprint = @Fingerprint;

    IF @ExistingId IS NOT NULL
    BEGIN
        SELECT @ExistingId AS RecordId, CAST(1 AS bit) AS AlreadyExisted;
        RETURN;
    END;

    INSERT INTO dbo.IntegrationInbox (SourceTable, SourceQuery, PrimaryKeyValue, WatermarkUtc, Fingerprint, PayloadFormat, Payload, ReceivedAtUtc)
    VALUES (@SourceTable, @SourceQuery, @PrimaryKeyValue, @WatermarkUtc, @Fingerprint, @PayloadFormat, @Payload, @ReceivedAtUtc);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS RecordId, CAST(0 AS bit) AS AlreadyExisted;
END;
