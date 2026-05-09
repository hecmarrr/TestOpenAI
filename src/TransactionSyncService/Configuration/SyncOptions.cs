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

    [Required]
    public string SourceReadProcedure { get; set; } = "dbo.sp_Sync_GetPendingRecords";

    [Required]
    public string StateGetCursorProcedure { get; set; } = "dbo.sp_Sync_GetCheckpoint";

    [Required]
    public string StateHasFingerprintProcedure { get; set; } = "dbo.sp_Sync_HasDeliveredFingerprint";

    [Required]
    public string StateUpsertProcedure { get; set; } = "dbo.sp_Sync_UpsertDeliveryState";

    public string AuthToken { get; set; } = string.Empty;

    public PayloadFormat DefaultPayloadFormat { get; set; } = PayloadFormat.Xml;

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

    public string Route { get; set; } = "api/inbox/records";

    public bool Enabled { get; set; } = true;

    public PayloadFormat? PayloadFormat { get; set; }
}

public enum PayloadFormat
{
    Json = 1,
    Xml = 2
}
