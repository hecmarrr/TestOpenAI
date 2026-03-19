namespace TransactionSyncCentralApi.Models;

public sealed class RecordEnvelope
{
    public required string SourceTable { get; init; }
    public required string PrimaryKeyValue { get; init; }
    public required DateTime WatermarkUtc { get; init; }
    public required string Fingerprint { get; init; }
    public required PayloadFormat PayloadFormat { get; init; }
    public required string Payload { get; init; }
    public required Dictionary<string, object?> Data { get; init; }
}

public sealed class StoreResult
{
    public required long RecordId { get; init; }
    public required bool AlreadyExisted { get; init; }
}

public enum PayloadFormat
{
    Json = 1,
    Xml = 2
}
