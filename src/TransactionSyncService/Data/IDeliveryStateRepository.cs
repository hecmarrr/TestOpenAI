using TransactionSyncService.Models;

namespace TransactionSyncService.Data;

public interface IDeliveryStateRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<SyncCursor> GetCursorAsync(string tableName, CancellationToken cancellationToken);
    Task<bool> HasDeliveredFingerprintAsync(string tableName, string fingerprint, CancellationToken cancellationToken);
    Task MarkDeliveredAsync(string tableName, OutboundRecord record, string payload, string fingerprint, CancellationToken cancellationToken);
    Task MarkFailedAsync(string tableName, OutboundRecord record, string payload, string fingerprint, string error, CancellationToken cancellationToken);
}
