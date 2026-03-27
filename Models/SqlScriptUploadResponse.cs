namespace SqlProcedureDeployer.Models;

public sealed class SqlScriptUploadResponse
{
    public Guid ScriptId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; init; }
    public string Message { get; init; } = string.Empty;
}
