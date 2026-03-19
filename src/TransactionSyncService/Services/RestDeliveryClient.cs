using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Services;

public sealed class RestDeliveryClient(HttpClient httpClient, IOptions<SyncOptions> options) : IDeliveryClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient = httpClient;
    private readonly SyncOptions _options = options.Value;

    public async Task SendAsync(TableSyncDefinition table, RecordEnvelope envelope, CancellationToken cancellationToken)
    {
        var route = table.Route.TrimStart('/');
        var endpoint = $"{_options.RestEndpoint.TrimEnd('/')}/{route}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Idempotency-Key", envelope.Fingerprint);

        if (!string.IsNullOrWhiteSpace(_options.AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AuthToken);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(envelope, SerializerOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
