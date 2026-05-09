using System.Data;
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
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<StoreResult>(new CommandDefinition(
            _options.StoreInboxProcedure,
            new
            {
                envelope.SourceTable,
                envelope.SourceQuery,
                envelope.PrimaryKeyValue,
                envelope.WatermarkUtc,
                envelope.Fingerprint,
                PayloadFormat = envelope.PayloadFormat.ToString(),
                envelope.Payload,
                ReceivedAtUtc = DateTime.UtcNow
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }
}
