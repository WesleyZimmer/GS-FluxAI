using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FluxAI.Desktop.Api;

internal sealed class FluxAiApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public FluxAiApiClient(string baseUrl, string operatorToken)
    {
        var normalized = baseUrl.Trim().TrimEnd('/') + "/";
        _http = new HttpClient
        {
            BaseAddress = new Uri(normalized, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20)
        };
        if (!string.IsNullOrWhiteSpace(operatorToken))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken.Trim());
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("healthz", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ScenarioItem>> GetScenariosAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<ScenarioItem>();
        string? pageToken = null;
        do
        {
            var query = "api/v1/scenarios?pageSize=100";
            if (!string.IsNullOrWhiteSpace(pageToken))
                query += "&pageToken=" + Uri.EscapeDataString(pageToken);

            var page = await GetAsync<ScenarioPage>(query, cancellationToken);
            all.AddRange(page.Items);
            pageToken = page.Page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return all;
    }

    public async Task<IReadOnlyList<SignalControllerItem>> GetSignalControllersAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var all = new List<SignalControllerItem>();
        string? pageToken = null;
        do
        {
            var query = $"api/v1/signal-controllers?scenarioId={Uri.EscapeDataString(scenarioId)}&pageSize=100";
            if (!string.IsNullOrWhiteSpace(pageToken))
                query += "&pageToken=" + Uri.EscapeDataString(pageToken);

            var page = await GetAsync<SignalControllerPage>(query, cancellationToken);
            all.AddRange(page.Items);
            pageToken = page.Page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return all;
    }

    public Task<ConfigurationResponse> CreateConfigurationAsync(
        ConfigurationWriteRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync<ConfigurationWriteRequest, ConfigurationResponse>(
            "api/v1/configurations", request, cancellationToken);

    public Task<ConfigurationResponse> GetConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default) =>
        GetAsync<ConfigurationResponse>($"api/v1/configurations/{configurationId:D}", cancellationToken);

    public async Task<ConfigurationResponse> ReplaceConfigurationAsync(
        Guid configurationId,
        int expectedRevision,
        ConfigurationWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"api/v1/configurations/{configurationId:D}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.TryAddWithoutValidation("If-Match", $"\"{expectedRevision}\"");

        using var response = await _http.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConfigurationResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("A API atualizou a configuração, mas não retornou o recurso esperado.");
    }

    public Task<TrainingRun> StartRunAsync(
        RunRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<RunRequest, TrainingRun>(
            "api/v1/runs", request, cancellationToken, idempotencyKey);

    public Task<TrainingRun> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        GetAsync<TrainingRun>($"api/v1/runs/{runId:D}", cancellationToken);

    public async Task<IReadOnlyList<TrainingRun>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<TrainingRun>();
        string? pageToken = null;
        do
        {
            var query = "api/v1/runs?pageSize=50";
            if (!string.IsNullOrWhiteSpace(pageToken))
                query += "&pageToken=" + Uri.EscapeDataString(pageToken);

            var page = await GetAsync<RunPage>(query, cancellationToken);
            all.AddRange(page.Items);
            pageToken = page.Page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return all;
    }

    public async Task<(IReadOnlyList<MetricPoint> Items, MetricCoverage? Coverage)> GetRunMetricsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var all = new List<MetricPoint>();
        string? pageToken = null;
        MetricCoverage? coverage = null;
        do
        {
            var query = $"api/v1/runs/{runId:D}/metrics?pageSize=100";
            if (!string.IsNullOrWhiteSpace(pageToken))
                query += "&pageToken=" + Uri.EscapeDataString(pageToken);

            var page = await GetAsync<MetricPage>(query, cancellationToken);
            all.AddRange(page.Items);
            coverage ??= page.Coverage;
            pageToken = page.Page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return (all, coverage);
    }

    public async Task<IReadOnlyList<SignalResultItem>> GetSignalResultsAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var all = new List<SignalResultItem>();
        string? pageToken = null;
        do
        {
            var query = $"api/v1/runs/{runId:D}/signal-results?pageSize=100";
            if (!string.IsNullOrWhiteSpace(pageToken))
                query += "&pageToken=" + Uri.EscapeDataString(pageToken);

            var page = await GetAsync<SignalResultPage>(query, cancellationToken);
            all.AddRange(page.Items);
            pageToken = page.Page.NextPageToken;
        } while (!string.IsNullOrWhiteSpace(pageToken));

        return all;
    }

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("A API retornou um corpo vazio ou inválido.");
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var response = await _http.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("A API aceitou a chamada, mas não retornou o recurso esperado.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiRequestException(response.StatusCode, DescribeError(response.StatusCode, body));
    }

    private static string DescribeError(HttpStatusCode statusCode, string body)
    {
        var status = (int)statusCode;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var title = root.TryGetProperty("title", out var titleNode) ? titleNode.GetString() : null;
            var detail = root.TryGetProperty("detail", out var detailNode) ? detailNode.GetString() : null;
            var errors = new List<string>();
            if (root.TryGetProperty("errors", out var errorsNode) && errorsNode.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsNode.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;
                    errors.AddRange(property.Value.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => $"{property.Name}: {item.GetString()}"));
                }
            }

            var message = string.Join(" ", new[] { title, detail }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            if (errors.Count > 0)
                message += " " + string.Join(" ", errors);
            return $"HTTP {status}: {(string.IsNullOrWhiteSpace(message) ? "a API recusou a solicitação." : message)}";
        }
        catch (JsonException)
        {
            var suffix = string.IsNullOrWhiteSpace(body) ? "" : " " + body[..Math.Min(body.Length, 300)];
            return $"HTTP {status}: a API recusou a solicitação.{suffix}";
        }
    }

    public void Dispose() => _http.Dispose();
}
