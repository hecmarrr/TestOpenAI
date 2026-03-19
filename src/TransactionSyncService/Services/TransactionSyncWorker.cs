using Microsoft.Extensions.Options;
using TransactionSyncService.Configuration;
using TransactionSyncService.Data;
using TransactionSyncService.Models;

namespace TransactionSyncService.Services;

public sealed class TransactionSyncWorker(
    ILogger<TransactionSyncWorker> logger,
    IOptions<SyncOptions> options,
    ISourceRepository sourceRepository,
    IDeliveryStateRepository deliveryStateRepository,
    IDeliveryClient deliveryClient) : BackgroundService
{
    private readonly ILogger<TransactionSyncWorker> _logger = logger;
    private readonly SyncOptions _options = options.Value;
    private readonly ISourceRepository _sourceRepository = sourceRepository;
    private readonly IDeliveryStateRepository _deliveryStateRepository = deliveryStateRepository;
    private readonly IDeliveryClient _deliveryClient = deliveryClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _deliveryStateRepository.InitializeAsync(stoppingToken);
        _logger.LogInformation("Servicio de sincronización iniciado. Tablas configuradas: {Count}", _options.Tables.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var table in _options.Tables)
            {
                try
                {
                    await ProcessTableAsync(table, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando la tabla {TableName}", table.TableName);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessTableAsync(TableSyncDefinition table, CancellationToken cancellationToken)
    {
        var cursor = await _deliveryStateRepository.GetCursorAsync(table.TableName, cancellationToken);
        var records = await _sourceRepository.ReadPendingAsync(table, cursor, _options.BatchSize, cancellationToken);
        var payloadFormat = table.PayloadFormat ?? _options.DefaultPayloadFormat;

        foreach (var record in records)
        {
            var payload = record.Serialize(payloadFormat);
            var fingerprint = record.ComputeFingerprint(payloadFormat);

            if (await _deliveryStateRepository.HasDeliveredFingerprintAsync(table.TableName, fingerprint, cancellationToken))
            {
                _logger.LogInformation("Registro omitido por duplicidad. Tabla={TableName}, Id={Id}", table.TableName, record.PrimaryKeyValue);
                continue;
            }

            var envelope = new RecordEnvelope
            {
                SourceTable = table.TableName,
                PrimaryKeyValue = record.PrimaryKeyValue,
                WatermarkUtc = record.WatermarkValueUtc,
                Fingerprint = fingerprint,
                PayloadFormat = payloadFormat,
                Payload = payload,
                Data = new Dictionary<string, object?>(record.Data, StringComparer.OrdinalIgnoreCase)
            };

            try
            {
                await _deliveryClient.SendAsync(table, envelope, cancellationToken);
                await _deliveryStateRepository.MarkDeliveredAsync(table.TableName, record, payload, fingerprint, cancellationToken);
                _logger.LogInformation("Registro enviado correctamente. Tabla={TableName}, Id={Id}", table.TableName, record.PrimaryKeyValue);
            }
            catch (Exception ex)
            {
                await _deliveryStateRepository.MarkFailedAsync(table.TableName, record, payload, fingerprint, ex.Message, cancellationToken);
                _logger.LogError(ex, "Falló el envío del registro. Tabla={TableName}, Id={Id}", table.TableName, record.PrimaryKeyValue);
            }
        }
    }
}
