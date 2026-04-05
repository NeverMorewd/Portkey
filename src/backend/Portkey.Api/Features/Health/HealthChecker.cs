using System.Net.Http.Headers;
using System.Net.Sockets;

namespace Portkey.Api.Features.Health;

public enum CheckType { Tcp, Http }

public record HealthCheckResult(bool Healthy, string Reason, DateTime CheckedAt);

public class HealthChecker
{
    private readonly IHttpClientFactory _httpFactory;

    public HealthChecker(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<HealthCheckResult> CheckAsync(
        string address, int port, CheckType type = CheckType.Tcp,
        CancellationToken ct = default)
    {
        return type == CheckType.Http
            ? await HttpCheckAsync(address, port, ct)
            : await TcpCheckAsync(address, port, ct);
    }

    private static async Task<HealthCheckResult> TcpCheckAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, cts.Token);
            return new HealthCheckResult(true, "TCP connection succeeded", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, $"TCP: {ex.Message}", DateTime.UtcNow);
        }
    }

    private async Task<HealthCheckResult> HttpCheckAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("health");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var url = $"http://{host}:{port}/health";
            var response = await client.GetAsync(url, cts.Token);
            return response.IsSuccessStatusCode
                ? new HealthCheckResult(true, $"HTTP {(int)response.StatusCode}", DateTime.UtcNow)
                : new HealthCheckResult(false, $"HTTP {(int)response.StatusCode}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, $"HTTP: {ex.Message}", DateTime.UtcNow);
        }
    }
}
