using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Data;

public sealed class SqlServerDeliveryStateRepository(IOptions<SyncOptions> options) : IDeliveryStateRepository
{
    private readonly SyncOptions _options = options.Value;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
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
            """;

        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<SyncCursor> GetCursorAsync(string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastWatermarkUtc, LastPrimaryKeyValue
            FROM dbo.SyncCheckpoint
            WHERE TableName = @TableName;
            """;

        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync(sql, new { TableName = tableName });
        return row is null
            ? new SyncCursor()
            : new SyncCursor
            {
                LastWatermarkUtc = row.LastWatermarkUtc,
                LastPrimaryKeyValue = row.LastPrimaryKeyValue
            };
    }

    public async Task<bool> HasDeliveredFingerprintAsync(string tableName, string fingerprint, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.SyncDeliveryLog
            WHERE TableName = @TableName
              AND Fingerprint = @Fingerprint
              AND DeliveryStatus = 'Delivered';
            """;

        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { TableName = tableName, Fingerprint = fingerprint }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public Task MarkDeliveredAsync(string tableName, OutboundRecord record, string payload, string fingerprint, CancellationToken cancellationToken) =>
        UpsertAsync(tableName, record, payload, fingerprint, "Delivered", null, updateCheckpoint: true, cancellationToken);

    public Task MarkFailedAsync(string tableName, OutboundRecord record, string payload, string fingerprint, string error, CancellationToken cancellationToken) =>
        UpsertAsync(tableName, record, payload, fingerprint, "Failed", error, updateCheckpoint: false, cancellationToken);

    private async Task UpsertAsync(
        string tableName,
        OutboundRecord record,
        string payload,
        string fingerprint,
        string status,
        string? error,
        bool updateCheckpoint,
        CancellationToken cancellationToken)
    {
        const string logSql = """
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
            """;

        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(logSql, new
        {
            TableName = tableName,
            record.PrimaryKeyValue,
            WatermarkUtc = record.WatermarkValueUtc,
            Fingerprint = fingerprint,
            Payload = payload,
            DeliveryStatus = status,
            LastError = error,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken));

        if (!updateCheckpoint)
        {
            return;
        }

        const string checkpointSql = """
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
            """;

        await connection.ExecuteAsync(new CommandDefinition(checkpointSql, new
        {
            TableName = tableName,
            LastWatermarkUtc = record.WatermarkValueUtc,
            LastPrimaryKeyValue = record.PrimaryKeyValue,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken));
    }
}
