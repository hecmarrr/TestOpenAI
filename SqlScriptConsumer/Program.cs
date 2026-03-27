using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

if (args.Length < 3)
{
    Console.WriteLine("Uso: SqlScriptConsumer <apiBaseUrl> <scriptId> <sqlConnectionString> [targetDatabase]");
    return;
}

var apiBaseUrl = args[0].TrimEnd('/');
if (!Guid.TryParse(args[1], out var scriptId))
{
    Console.WriteLine("El scriptId no es un GUID válido.");
    return;
}

var connectionString = args[2];
var targetDatabase = args.Length > 3 ? args[3] : null;

using var httpClient = new HttpClient();
var response = await httpClient.GetAsync($"{apiBaseUrl}/api/sql/scripts/{scriptId}");
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"No se pudo obtener el script. StatusCode: {(int)response.StatusCode}");
    return;
}

var payload = await response.Content.ReadFromJsonAsync<SqlScriptPayload>();
if (payload is null || string.IsNullOrWhiteSpace(payload.SqlContent))
{
    Console.WriteLine("El servicio no devolvió contenido SQL válido.");
    return;
}

var sqlBatches = SplitSqlBatches(payload.SqlContent);
if (sqlBatches.Count == 0)
{
    Console.WriteLine("No se encontraron lotes SQL a ejecutar.");
    return;
}

var builder = new SqlConnectionStringBuilder(connectionString);
if (!string.IsNullOrWhiteSpace(targetDatabase))
{
    builder.InitialCatalog = targetDatabase;
}

await using var connection = new SqlConnection(builder.ConnectionString);
await connection.OpenAsync();
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);

try
{
    foreach (var batch in sqlBatches)
    {
        await using var command = new SqlCommand(batch, connection, transaction)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 120
        };

        await command.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
    Console.WriteLine($"Script {payload.ScriptId} ejecutado correctamente en SQL Server.");
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    Console.WriteLine($"Error al ejecutar script en SQL Server: {ex.Message}");
}

return;

static IReadOnlyList<string> SplitSqlBatches(string sqlText)
{
    var goRegex = new Regex(@"^\s*GO\s*($|--.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    return goRegex
        .Split(sqlText)
        .Select(part => part.Trim())
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .ToList();
}

public sealed class SqlScriptPayload
{
    public Guid ScriptId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string SqlContent { get; init; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; init; }
}
