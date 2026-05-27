using BLL.Interfaces.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BLL.Services.Documents;

public class GeminiEmbeddingService : IEmbeddingService
{
    private const int ExpectedDimensions = 3072;
    private static readonly HttpClient HttpClient = new();

    private readonly ILogger<GeminiEmbeddingService> _logger;
    private readonly string[] _apiKeys;
    private readonly string _model;
    private int _keyIndex;

    public GeminiEmbeddingService(IConfiguration configuration, ILogger<GeminiEmbeddingService> logger)
    {
        _logger = logger;

        var apiKeysFromConfig = configuration.GetSection("Gemini:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        var apiKeysFromEnv = (Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        _apiKeys = apiKeysFromConfig
            .Concat(apiKeysFromEnv)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (_apiKeys.Length == 0)
        {
            throw new InvalidOperationException("Missing Gemini API keys. Set Gemini:ApiKeys or GEMINI_API_KEY.");
        }

        _model = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-1";
        _logger.LogInformation("Gemini embedding service initialized. Model={Model}, ApiKeyCount={ApiKeyCount}", _model, _apiKeys.Length);
    }

    public async Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Vector(new float[ExpectedDimensions]);
        }

        Exception? lastError = null;
        var attempts = _apiKeys.Length;

        for (var i = 0; i < attempts; i++)
        {
            var apiKeyIndex = GetNextApiKeyIndex();
            var apiKey = _apiKeys[apiKeyIndex];
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:embedContent?key={apiKey}";
                var request = new GeminiEmbedRequest
                {
                    Content = new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = text }]
                    },
                    TaskType = "RETRIEVAL_DOCUMENT",
                    OutputDimensionality = ExpectedDimensions
                };

                _logger.LogInformation("Gemini embedding request started. Attempt={Attempt}/{Attempts}, ApiKeyIndex={ApiKeyIndex}, Model={Model}, TextLength={TextLength}",
                    i + 1, attempts, apiKeyIndex, _model, text.Length);

                using var response = await HttpClient.PostAsJsonAsync(url, request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Gemini embedding failed. Attempt={Attempt}/{Attempts}, ApiKeyIndex={ApiKeyIndex}, StatusCode={StatusCode}, Body={Body}",
                        i + 1, attempts, apiKeyIndex, (int)response.StatusCode, Truncate(responseBody, 2000));

                    if ((int)response.StatusCode is 401 or 403 or 429 or 400)
                    {
                        lastError = new InvalidOperationException(
                            $"Gemini API returned {(int)response.StatusCode} for key index {apiKeyIndex}. Body: {Truncate(responseBody, 1000)}");
                        continue;
                    }

                    throw new InvalidOperationException($"Gemini embedding failed: {response.StatusCode} - {responseBody}");
                }

                var payload = await response.Content.ReadFromJsonAsync<GeminiEmbedResponse>(cancellationToken: cancellationToken);
                var values = payload?.Embedding?.Values ?? [];
                var floats = values.Select(v => (float)v).ToArray();
                _logger.LogInformation("Gemini embedding succeeded. ApiKeyIndex={ApiKeyIndex}, Dimension={Dimension}", apiKeyIndex, floats.Length);
                return new Vector(floats.Length == 0 ? new float[ExpectedDimensions] : floats);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Gemini embedding HTTP error. ApiKeyIndex={ApiKeyIndex}", apiKeyIndex);
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Gemini embedding operation error. ApiKeyIndex={ApiKeyIndex}", apiKeyIndex);
            }
        }

        _logger.LogError(lastError, "Gemini embedding failed after rotating through all API keys. Model={Model}, ApiKeyCount={ApiKeyCount}", _model, _apiKeys.Length);
        throw new InvalidOperationException("Gemini embedding failed after rotating through all API keys.", lastError);
    }

    private int GetNextApiKeyIndex()
    {
        var index = Interlocked.Increment(ref _keyIndex);
        return index % _apiKeys.Length;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private sealed class GeminiEmbedRequest
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        [JsonPropertyName("taskType")]
        public string? TaskType { get; set; }

        [JsonPropertyName("outputDimensionality")]
        public int? OutputDimensionality { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiEmbedResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    private sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<double> Values { get; set; } = new();
    }
}
