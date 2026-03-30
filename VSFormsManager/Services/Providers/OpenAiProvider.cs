using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VSFormsManager.Services.Providers
{
    /// <summary>
    /// IAiProvider implementation for OpenAI GPT-4o.
    /// Uses the /v1/chat/completions endpoint with system + user message roles.
    /// </summary>
    public class OpenAiProvider : IAiProvider
    {
        public string ProviderName => "GPT-4o (OpenAI)";

        private readonly string _apiKey;
        private readonly string _model;

        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public OpenAiProvider(string apiKey, string model)
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
                    "OpenAI API key is not configured. Add it in AI Provider Settings.");

            var payload = new
            {
                model      = _model,
                max_tokens = 8000,
                messages   = new[]
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
                .ReadFromJsonAsync<OpenAiResponse>(cancellationToken: cancellationToken);

            if (result?.Error != null)
                throw new InvalidOperationException(
                    $"OpenAI API error ({result.Error.Type}): {result.Error.Message}");

            return result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        // ── Response DTOs ─────────────────────────────────────────────────────

        private record OpenAiResponse(
            [property: JsonPropertyName("choices")] OpenAiChoice[]? Choices,
            [property: JsonPropertyName("error")]   OpenAiError?    Error);

        private record OpenAiChoice(
            [property: JsonPropertyName("message")] OpenAiMessage Message);

        private record OpenAiMessage(
            [property: JsonPropertyName("role")]    string Role,
            [property: JsonPropertyName("content")] string Content);

        private record OpenAiError(
            [property: JsonPropertyName("type")]    string Type,
            [property: JsonPropertyName("message")] string Message);
    }
}
