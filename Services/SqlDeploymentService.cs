using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using SqlProcedureDeployer.Models;

namespace SqlProcedureDeployer.Services;

public sealed class SqlDeploymentService(IConfiguration configuration, ILogger<SqlDeploymentService> logger) : ISqlDeploymentService
{
    private static readonly Regex GoRegex = new(@"^\s*GO\s*($|--.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<DeploySqlResponse> DeployProcedureScriptAsync(Stream sqlFileStream, string fileName, string? targetDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqlFileStream);

        var sqlText = await ReadSqlAsync(sqlFileStream, cancellationToken);
        ValidateSqlScript(sqlText, fileName);

        var batches = SplitSqlBatches(sqlText);
        if (batches.Count == 0)
        {
            throw new InvalidOperationException("No se encontraron bloques SQL ejecutables en el archivo.");
        }

        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ConnectionStrings:SqlServer'.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(targetDatabase))
        {
            builder.InitialCatalog = targetDatabase;
        }

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            foreach (var batch in batches)
            {
                await using var command = new SqlCommand(batch, connection, transaction)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 120
                };

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new DeploySqlResponse
            {
                Success = true,
                Message = "El procedimiento almacenado se desplegó correctamente.",
                ProcedureName = TryExtractProcedureName(sqlText),
                ExecutedBatches = batches
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error al ejecutar el script SQL {FileName}", fileName);
            throw;
        }
    }

    private static async Task<string> ReadSqlAsync(Stream sqlFileStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(sqlFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static void ValidateSqlScript(string sqlText, string fileName)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            throw new InvalidOperationException($"El archivo '{fileName}' está vacío.");
        }

        var normalized = sqlText.TrimStart();
        if (!normalized.StartsWith("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo se permite desplegar scripts que creen o alteren procedimientos almacenados.");
        }

        var forbiddenTokens = new[]
        {
            "DROP DATABASE", "ALTER LOGIN", "CREATE LOGIN", "DROP LOGIN", "CREATE USER", "DROP USER",
            "xp_cmdshell", "sp_configure"
        };

        foreach (var token in forbiddenTokens)
        {
            if (sqlText.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"El script contiene una instrucción no permitida: {token}");
            }
        }
    }

    private static IReadOnlyList<string> SplitSqlBatches(string sqlText)
    {
        var parts = GoRegex.Split(sqlText)
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts;
    }

    private static string? TryExtractProcedureName(string sqlText)
    {
        var match = Regex.Match(sqlText,
            @"\b(?:CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+PROCEDURE\s+(?<name>(?:\[[^\]]+\]|\w+)\.(?:\[[^\]]+\]|\w+))",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["name"].Value : null;
    }
}
