using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using TransactionSyncService.Configuration;
using TransactionSyncService.Models;

namespace TransactionSyncService.Services;

public sealed class RestDeliveryClient(HttpClient httpClient, IOptions<SyncOptions> options) : IDeliveryClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly SyncOptions _options = options.Value;

    public async Task SendAsync(TableSyncDefinition table, RecordEnvelope envelope, CancellationToken cancellationToken)
    {
        var route = table.Route.TrimStart('/');
        var endpoint = $"{_options.RestEndpoint.TrimEnd('/')}/{route}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Headers.Add("X-Idempotency-Key", envelope.Fingerprint);
        request.Headers.Add("X-Source-Table", envelope.SourceTable);

        if (!string.IsNullOrWhiteSpace(_options.AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AuthToken);
        }

        request.Content = new StringContent(
            envelope.Payload,
            Encoding.UTF8,
            "application/xml");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
