using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Data;

public sealed class SqlServerSourceRepository(IOptions<SyncOptions> options) : ISourceRepository
{
    private readonly SyncOptions _options = options.Value;

    public async Task<IReadOnlyList<OutboundRecord>> ReadPendingAsync(
        TableSyncDefinition table,
        SyncCursor cursor,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT TOP (@BatchSize) *
            FROM {SqlIdentifier.QuoteTableName(table.TableName)}
            WHERE (
                @LastWatermarkUtc IS NULL
                OR {SqlIdentifier.QuoteIdentifier(table.WatermarkColumn)} > @LastWatermarkUtc
                OR (
                    {SqlIdentifier.QuoteIdentifier(table.WatermarkColumn)} = @LastWatermarkUtc
                    AND CONVERT(NVARCHAR(256), {SqlIdentifier.QuoteIdentifier(table.PrimaryKeyColumn)}) > @LastPrimaryKeyValue
                )
            )
            ORDER BY {SqlIdentifier.QuoteIdentifier(table.WatermarkColumn)}, {SqlIdentifier.QuoteIdentifier(table.PrimaryKeyColumn)};
            """;

        var records = new List<OutboundRecord>();

        await using var connection = new SqlConnection(_options.SourceConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@LastWatermarkUtc", (object?)cursor.LastWatermarkUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("@LastPrimaryKeyValue", (object?)cursor.LastPrimaryKeyValue ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var data = ReadRow(reader);
            records.Add(new OutboundRecord
            {
                TableName = table.TableName,
                SourceQuery = BuildSourceQuery(table, data),
                PrimaryKeyValue = Convert.ToString(data[table.PrimaryKeyColumn]) ?? string.Empty,
                WatermarkValueUtc = EnsureUtc(Convert.ToDateTime(data[table.WatermarkColumn])),
                Data = data
            });
        }

        return records;
    }

    private static Dictionary<string, object?> ReadRow(DbDataReader reader)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            result[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return result;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string BuildSourceQuery(TableSyncDefinition table, IReadOnlyDictionary<string, object?> data)
    {
        var primaryKeyValue = Convert.ToString(data[table.PrimaryKeyColumn]) ?? string.Empty;
        var quotedPk = primaryKeyValue.Replace("'", "''");
        return $"SELECT * FROM {table.TableName} WHERE {table.PrimaryKeyColumn} = '{quotedPk}'";
    }
}
