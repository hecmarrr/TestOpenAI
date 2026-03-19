using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml;
using TransactionSyncService.Configuration;

namespace TransactionSyncService.Models;

public sealed class OutboundRecord
{
    public required string TableName { get; init; }
    public required string SourceQuery { get; init; }
    public required string PrimaryKeyValue { get; init; }
    public required DateTime WatermarkValueUtc { get; init; }
    public required IReadOnlyDictionary<string, object?> Data { get; init; }

    public string Serialize(PayloadFormat payloadFormat, string fingerprint) =>
        payloadFormat == PayloadFormat.Xml ? SerializeXml(fingerprint) : JsonSerializer.Serialize(Data);

    public string ComputeFingerprint(PayloadFormat payloadFormat)
    {
        var payload = payloadFormat == PayloadFormat.Xml
            ? SerializeRowXml()
            : JsonSerializer.Serialize(Data);
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes($"{TableName}|{SourceQuery}|{PrimaryKeyValue}|{WatermarkValueUtc:O}|{payload}");
        return Convert.ToHexString(sha256.ComputeHash(bytes));
    }

    private string SerializeXml(string fingerprint)
    {
        var root = new XElement("SyncEnvelope",
            new XElement("sourceTable", TableName),
            new XElement("sourceQuery", new XCData(SourceQuery)),
            new XElement("primaryKeyValue", PrimaryKeyValue),
            new XElement("watermarkUtc", WatermarkValueUtc.ToString("O")),
            new XElement("fingerprint", fingerprint),
            new XElement("payloadFormat", PayloadFormat.Xml.ToString()),
            new XElement("record", XElement.Parse(SerializeRowXml())));

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private string SerializeRowXml()
    {
        var root = new XElement("Record",
            new XAttribute("table", TableName),
            new XAttribute("primaryKey", PrimaryKeyValue),
            new XAttribute("watermarkUtc", WatermarkValueUtc.ToString("O")),
            Data.Select(kvp => new XElement(XmlConvert.EncodeName(kvp.Key), kvp.Value ?? string.Empty)));

        return root.ToString(SaveOptions.DisableFormatting);
    }
}

public sealed class SyncCursor
{
    public DateTime? LastWatermarkUtc { get; init; }
    public string? LastPrimaryKeyValue { get; init; }
}

public sealed class RecordEnvelope
{
    public required string SourceTable { get; init; }
    public required string SourceQuery { get; init; }
    public required string PrimaryKeyValue { get; init; }
    public required DateTime WatermarkUtc { get; init; }
    public required string Fingerprint { get; init; }
    public required PayloadFormat PayloadFormat { get; init; }
    public required string Payload { get; init; }
}
