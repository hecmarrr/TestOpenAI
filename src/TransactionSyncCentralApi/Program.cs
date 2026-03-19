using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using TransactionSyncCentralApi.Configuration;
using TransactionSyncCentralApi.Data;
using TransactionSyncCentralApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<CentralApiOptions>()
    .Bind(builder.Configuration.GetSection(CentralApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IInboundRecordRepository, SqlServerInboundRecordRepository>();

var app = builder.Build();

app.MapPost("/api/inbox/records", async (
    HttpRequest request,
    IInboundRecordRepository repository,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var xmlPayload = await reader.ReadToEndAsync(cancellationToken);
    var envelope = ParseEnvelope(xmlPayload);
    var result = await repository.StoreAsync(envelope, cancellationToken);
    return result.AlreadyExisted
        ? Results.Ok(new { status = "duplicate", id = result.RecordId })
        : Results.Created($"/api/inbox/records/{result.RecordId}", new { status = "stored", id = result.RecordId });
});

app.Run();

static RecordEnvelope ParseEnvelope([Required] string xmlPayload)
{
    var document = XDocument.Parse(xmlPayload);
    var root = document.Root ?? throw new InvalidOperationException("XML payload is missing a root element.");

    return new RecordEnvelope
    {
        SourceTable = root.Element("sourceTable")?.Value ?? throw new InvalidOperationException("Missing sourceTable."),
        SourceQuery = root.Element("sourceQuery")?.Value ?? throw new InvalidOperationException("Missing sourceQuery."),
        PrimaryKeyValue = root.Element("primaryKeyValue")?.Value ?? throw new InvalidOperationException("Missing primaryKeyValue."),
        WatermarkUtc = DateTime.Parse(root.Element("watermarkUtc")?.Value ?? throw new InvalidOperationException("Missing watermarkUtc.")),
        Fingerprint = root.Element("fingerprint")?.Value ?? throw new InvalidOperationException("Missing fingerprint."),
        PayloadFormat = Enum.Parse<PayloadFormat>(root.Element("payloadFormat")?.Value ?? nameof(PayloadFormat.Xml), ignoreCase: true),
        Payload = xmlPayload
    };
}
