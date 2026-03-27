namespace SqlProcedureDeployer.Services;

public sealed class StoredSqlScript
{
    public Guid ScriptId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string SqlContent { get; init; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; init; }
}
