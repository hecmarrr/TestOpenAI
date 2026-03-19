using System.ComponentModel.DataAnnotations;

namespace TransactionSyncService.Configuration;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    [Range(1, 3600)]
    public int PollingIntervalSeconds { get; set; } = 30;

    [Range(1, 5000)]
    public int BatchSize { get; set; } = 100;

    [Required]
    public string SourceConnectionString { get; set; } = string.Empty;

    [Required]
    public string LocalStateConnectionString { get; set; } = string.Empty;

    [Required]
    public string RestEndpoint { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public PayloadFormat DefaultPayloadFormat { get; set; } = PayloadFormat.Json;

    [MinLength(1)]
    public List<TableSyncDefinition> Tables { get; set; } = [];
}

public sealed class TableSyncDefinition
{
    [Required]
    public string TableName { get; set; } = string.Empty;

    [Required]
    public string PrimaryKeyColumn { get; set; } = string.Empty;

    [Required]
    public string WatermarkColumn { get; set; } = string.Empty;

    [MinLength(1)]
    public List<string> Columns { get; set; } = [];

    public string Route { get; set; } = "api/inbox/records";

    public PayloadFormat? PayloadFormat { get; set; }
}

public enum PayloadFormat
{
    Json = 1,
    Xml = 2
}
