using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VSFormsManager.Services.Providers
{
    /// <summary>
    /// IAiProvider implementation for Mistral AI.
    /// Uses the /v1/chat/completions endpoint (OpenAI-compatible format).
    /// Default model: mistral-large-latest.
    /// </summary>
    public class MistralProvider : IAiProvider
    {
        public string ProviderName => "Mistral AI";

        private readonly string _apiKey;
        private readonly string _model;

        private const string ApiUrl = "https://api.mistral.ai/v1/chat/completions";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public MistralProvider(string apiKey, string model)
        {
            _apiKey = apiKey;
            _model  = model;
        }

        public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string> GenerateAsync(
            string            systemPrompt,
            string            userPrompt,
            CancellationToken cancellationToken)
        {
            if (!HasApiKey)
                throw new InvalidOperationException(
                    "Mistral API key is not configured. Add it in AI Provider Settings.");

            var payload = new
            {
                model    = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(payload);

            var response = await Http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<MistralResponse>(cancellationToken: cancellationToken);

            if (result?.Error != null)
                throw new InvalidOperationException(
                    $"Mistral API error ({result.Error.Type}): {result.Error.Message}");

            return result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? throw new InvalidOperationException("Mistral returned an empty response.");
        }

        // ── Response DTOs ─────────────────────────────────────────────────────

        private record MistralResponse(
            [property: JsonPropertyName("choices")] MistralChoice[]? Choices,
            [property: JsonPropertyName("error")]   MistralError?    Error);

        private record MistralChoice(
            [property: JsonPropertyName("message")] MistralMessage Message);

        private record MistralMessage(
            [property: JsonPropertyName("role")]    string Role,
            [property: JsonPropertyName("content")] string Content);

        private record MistralError(
            [property: JsonPropertyName("type")]    string Type,
            [property: JsonPropertyName("message")] string Message);
    }
}
