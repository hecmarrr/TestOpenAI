using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TransactionSyncCentralApi.Configuration;
using TransactionSyncCentralApi.Models;

namespace TransactionSyncCentralApi.Data;

public sealed class SqlServerInboundRecordRepository(IOptions<CentralApiOptions> options) : IInboundRecordRepository
{
    private readonly CentralApiOptions _options = options.Value;

    public async Task<StoreResult> StoreAsync(RecordEnvelope envelope, CancellationToken cancellationToken)
    {
        const string bootstrapSql = """
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

            IF COL_LENGTH('dbo.IntegrationInbox', 'SourceQuery') IS NULL
            BEGIN
                ALTER TABLE dbo.IntegrationInbox
                ADD SourceQuery NVARCHAR(MAX) NOT NULL CONSTRAINT DF_IntegrationInbox_SourceQuery DEFAULT ('');
            END;
            """;

        const string duplicateSql = """
            SELECT TOP 1 Id
            FROM dbo.IntegrationInbox
            WHERE Fingerprint = @Fingerprint;
            """;

        const string insertSql = """
            INSERT INTO dbo.IntegrationInbox (SourceTable, SourceQuery, PrimaryKeyValue, WatermarkUtc, Fingerprint, PayloadFormat, Payload, ReceivedAtUtc)
            VALUES (@SourceTable, @SourceQuery, @PrimaryKeyValue, @WatermarkUtc, @Fingerprint, @PayloadFormat, @Payload, @ReceivedAtUtc);

            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(bootstrapSql, cancellationToken: cancellationToken));

        var existingId = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(duplicateSql, new
        {
            envelope.Fingerprint
        }, cancellationToken: cancellationToken));

        if (existingId.HasValue)
        {
            return new StoreResult
            {
                RecordId = existingId.Value,
                AlreadyExisted = true
            };
        }

        var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(insertSql, new
        {
            envelope.SourceTable,
            envelope.SourceQuery,
            envelope.PrimaryKeyValue,
            envelope.WatermarkUtc,
            envelope.Fingerprint,
            PayloadFormat = envelope.PayloadFormat.ToString(),
            envelope.Payload,
            ReceivedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken));

        return new StoreResult
        {
            RecordId = id,
            AlreadyExisted = false
        };
    }
}
