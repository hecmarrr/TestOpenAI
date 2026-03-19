using TransactionSyncService.Configuration;
using TransactionSyncService.Data;
using TransactionSyncService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Transaction Sync Service";
});

builder.Services
    .AddOptions<SyncOptions>()
    .Bind(builder.Configuration.GetSection(SyncOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ISourceRepository, SqlServerSourceRepository>();
builder.Services.AddSingleton<IDeliveryStateRepository, SqlServerDeliveryStateRepository>();
builder.Services.AddHttpClient<IDeliveryClient, RestDeliveryClient>();
builder.Services.AddHostedService<TransactionSyncWorker>();

var host = builder.Build();
await host.RunAsync();
