using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Data;

public interface ISourceRepository
{
    Task<IReadOnlyList<OutboundRecord>> ReadPendingAsync(
        TableSyncDefinition table,
        SyncCursor cursor,
        int batchSize,
        CancellationToken cancellationToken);
}
