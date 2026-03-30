using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace VSFormsManager.Services.Providers
{
    /// <summary>
    /// IAiProvider implementation for Google Gemini.
    /// Uses the generateContent REST endpoint with a systemInstruction field.
    /// </summary>
    public class GeminiProvider : IAiProvider
    {
        public string ProviderName => "Gemini (Google)";

        private readonly string _apiKey;
        private readonly string _model;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(180) };

        public GeminiProvider(string apiKey, string model)
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
                    "Gemini API key is not configured. Add it in AI Provider Settings.");

            var modelSegment = _model.TrimStart('/');
            var url = $"https://generativelanguage.googleapis.com/v1beta/{modelSegment}" +
                      $":generateContent?key={_apiKey}";

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = userPrompt } } }
                }
            };

            var response = await Http.PostAsJsonAsync(url, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (result?.Error != null)
                throw new InvalidOperationException(
                    $"Gemini API error {result.Error.Code} ({result.Error.Status}): {result.Error.Message}");

            return result?.Candidates?.FirstOrDefault()
                         ?.Content?.Parts?.FirstOrDefault()?.Text
                   ?? throw new InvalidOperationException("Gemini returned an empty response.");
        }

        // ── Response DTOs ─────────────────────────────────────────────────────

        private record GeminiResponse(
            [property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates,
            [property: JsonPropertyName("error")]      GeminiError?       Error);

        private record GeminiCandidate(
            [property: JsonPropertyName("content")] GeminiContent Content);

        private record GeminiContent(
            [property: JsonPropertyName("parts")] GeminiPart[] Parts);

        private record GeminiPart(
            [property: JsonPropertyName("text")] string Text);

        private record GeminiError(
            [property: JsonPropertyName("code")]    int    Code,
            [property: JsonPropertyName("message")] string Message,
            [property: JsonPropertyName("status")]  string Status);
    }
}
