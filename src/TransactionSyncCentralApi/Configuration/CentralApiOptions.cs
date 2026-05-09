using System.ComponentModel.DataAnnotations;

namespace TransactionSyncCentralApi.Configuration;

public sealed class CentralApiOptions
{
    public const string SectionName = "CentralApi";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string StoreInboxProcedure { get; set; } = "dbo.sp_Sync_StoreIntegrationInbox";
}
