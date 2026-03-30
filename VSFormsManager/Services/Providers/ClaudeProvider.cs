using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VSFormsManager.Services.Providers
{
    /// <summary>
    /// IAiProvider implementation for Anthropic Claude.
    /// Calls the /v1/messages endpoint using the model stored in AppSettings.
    /// </summary>
    public class ClaudeProvider : IAiProvider
    {
        public string ProviderName => "Claude (Anthropic)";

        private readonly string _apiKey;
        private readonly string _model;

        private const string ApiUrl           = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public ClaudeProvider(string apiKey, string model)
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
                    "Claude API key is not configured. Add it in AI Provider Settings.");

            var payload = new
            {
                model      = _model,
                max_tokens = 8000,
                system     = systemPrompt,
                messages   = new[] { new { role = "user", content = userPrompt } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("x-api-key",         _apiKey);
            request.Headers.Add("anthropic-version", AnthropicVersion);
            request.Content = JsonContent.Create(payload);

            var response = await Http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);

            if (result?.Error != null)
                throw new InvalidOperationException(
                    $"Claude API error ({result.Error.Type}): {result.Error.Message}");

            return result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text
                   ?? throw new InvalidOperationException("Claude returned an empty response.");
        }

        // ── Response DTOs ─────────────────────────────────────────────────────

        private record ClaudeResponse(
            [property: JsonPropertyName("content")] ClaudeContent[]? Content,
            [property: JsonPropertyName("error")]   ClaudeError?     Error);

        private record ClaudeContent(
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("text")] string Text);

        private record ClaudeError(
            [property: JsonPropertyName("type")]    string Type,
            [property: JsonPropertyName("message")] string Message);
    }
}
