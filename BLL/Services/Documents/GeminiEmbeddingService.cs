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

        _model = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-2";
        _logger.LogDebug("Gemini embedding service initialized. Model={Model}, ApiKeyCount={Count}", _model, _apiKeys.Length);
    }

    public async Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Vector(new float[ExpectedDimensions]);
        }

        var results = await EmbedBatchAsync([text], cancellationToken);
        return results.FirstOrDefault() ?? new Vector(new float[ExpectedDimensions]);
    }

    public async Task<List<Vector>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        Exception? lastError = null;
        var attempts = Math.Max(_apiKeys.Length, 9);

        for (var i = 0; i < attempts; i++)
        {
            var apiKeyIndex = GetNextApiKeyIndex();
            var apiKey = _apiKeys[apiKeyIndex];
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents?key={apiKey}";
                var batchRequest = new GeminiBatchEmbedRequest
                {
                    Requests = texts.Select(text => new GeminiEmbedRequest
                    {
                        Model = $"models/{_model}",
                        Content = new GeminiContent
                        {
                            Parts = [new GeminiPart { Text = text }]
                        },
                        TaskType = "RETRIEVAL_DOCUMENT",
                        OutputDimensionality = ExpectedDimensions
                    }).ToList()
                };

                _logger.LogInformation("Gemini batch embedding request started. Attempt={Attempt}/{Attempts}, ApiKeyIndex={ApiKeyIndex}, Model={Model}, BatchSize={BatchSize}",
                    i + 1, attempts, apiKeyIndex, _model, texts.Count);

                using var response = await HttpClient.PostAsJsonAsync(url, batchRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Gemini batch embedding failed. Attempt={Attempt}/{Attempts}, ApiKeyIndex={ApiKeyIndex}, StatusCode={StatusCode}, Body={Body}",
                        i + 1, attempts, apiKeyIndex, (int)response.StatusCode, Truncate(responseBody, 2000));

                    if ((int)response.StatusCode is 401 or 403 or 429 or 400)
                    {
                        lastError = new InvalidOperationException(
                            $"Gemini API returned {(int)response.StatusCode} for key index {apiKeyIndex}. Body: {Truncate(responseBody, 1000)}");
                        
                        if ((int)response.StatusCode == 429)
                        {
                            // Nghỉ 5 giây trước khi thử lại key khác (hoặc key cũ) để API phục hồi
                            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        }
                        continue;
                    }

                    throw new InvalidOperationException($"Gemini batch embedding failed: {response.StatusCode} - {responseBody}");
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize<GeminiBatchEmbedResponse>(responseBody);
                var vectors = new List<Vector>();

                var returnedEmbeddings = payload?.Embeddings ?? [];
                
                for (int j = 0; j < texts.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(texts[j]))
                    {
                        vectors.Add(new Vector(new float[ExpectedDimensions]));
                        continue;
                    }

                    // Map the results assuming Gemini returns them in order, if some are missing we return empty
                    var values = returnedEmbeddings.ElementAtOrDefault(j)?.Values ?? [];
                    var floats = values.Select(v => (float)v).ToArray();
                    vectors.Add(new Vector(floats.Length == 0 ? new float[ExpectedDimensions] : floats));
                }

                _logger.LogInformation("Gemini batch embedding succeeded. ApiKeyIndex={ApiKeyIndex}, Returned={Returned}/{Requested}", apiKeyIndex, returnedEmbeddings.Count, texts.Count);
                return vectors;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Gemini batch embedding HTTP error. ApiKeyIndex={ApiKeyIndex}", apiKeyIndex);
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Gemini batch embedding operation error. ApiKeyIndex={ApiKeyIndex}", apiKeyIndex);
            }
        }

        _logger.LogError(lastError, "Gemini batch embedding failed after rotating through all API keys. Model={Model}, ApiKeyCount={ApiKeyCount}", _model, _apiKeys.Length);
        throw new InvalidOperationException("Gemini batch embedding failed after rotating through all API keys.", lastError);
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

    private sealed class GeminiBatchEmbedRequest
    {
        [JsonPropertyName("requests")]
        public List<GeminiEmbedRequest> Requests { get; set; } = [];
    }

    private sealed class GeminiEmbedRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

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
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiBatchEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<GeminiEmbedding> Embeddings { get; set; } = [];
    }

    private sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<double> Values { get; set; } = [];
    }
}
