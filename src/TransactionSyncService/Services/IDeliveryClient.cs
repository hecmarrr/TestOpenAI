using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Services;

public interface IDeliveryClient
{
    Task SendAsync(TableSyncDefinition table, RecordEnvelope envelope, CancellationToken cancellationToken);
}
