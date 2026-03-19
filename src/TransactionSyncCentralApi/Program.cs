using System.ComponentModel.DataAnnotations;
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
    [Required] RecordEnvelope envelope,
    IInboundRecordRepository repository,
    CancellationToken cancellationToken) =>
{
    var result = await repository.StoreAsync(envelope, cancellationToken);
    return result.AlreadyExisted
        ? Results.Ok(new { status = "duplicate", id = result.RecordId })
        : Results.Created($"/api/inbox/records/{result.RecordId}", new { status = "stored", id = result.RecordId });
});

app.Run();
