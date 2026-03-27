namespace SqlProcedureDeployer.Models;

public sealed class SqlScriptPayloadResponse
{
    public Guid ScriptId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string SqlContent { get; init; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; init; }
}
