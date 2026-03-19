using TransactionSyncCentralApi.Models;

namespace TransactionSyncCentralApi.Data;

public interface IInboundRecordRepository
{
    Task<StoreResult> StoreAsync(RecordEnvelope envelope, CancellationToken cancellationToken);
}
