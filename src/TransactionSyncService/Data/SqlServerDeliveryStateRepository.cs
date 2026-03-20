using System.Data;
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
        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
    }

    public async Task<SyncCursor> GetCursorAsync(string tableName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync(
            new CommandDefinition(
                _options.StateGetCursorProcedure,
                new { TableName = tableName },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
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
        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _options.StateHasFingerprintProcedure,
            new { TableName = tableName, Fingerprint = fingerprint },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
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
        await using var connection = new SqlConnection(_options.LocalStateConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_options.StateUpsertProcedure, new
        {
            TableName = tableName,
            record.PrimaryKeyValue,
            WatermarkUtc = record.WatermarkValueUtc,
            Fingerprint = fingerprint,
            Payload = payload,
            DeliveryStatus = status,
            LastError = error,
            LastWatermarkUtc = record.WatermarkValueUtc,
            LastPrimaryKeyValue = record.PrimaryKeyValue,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdateCheckpoint = updateCheckpoint
        },
        commandType: CommandType.StoredProcedure,
        cancellationToken: cancellationToken));
    }
}
