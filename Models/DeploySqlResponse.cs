namespace SqlProcedureDeployer.Models;

public sealed class DeploySqlResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ProcedureName { get; init; }
    public IReadOnlyList<string> ExecutedBatches { get; init; } = Array.Empty<string>();
}
