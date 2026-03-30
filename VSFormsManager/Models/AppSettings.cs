using VSFormsManager.Models;

namespace VSFormsManager.Models
{
    /// <summary>
    /// All user-configurable application settings.
    /// Persisted as JSON via <see cref="VSFormsManager.Services.SettingsManager"/>.
    /// </summary>
    public class AppSettings
    {
        // ── API Keys ──────────────────────────────────────────────────────────
        public string ClaudeApiKey  { get; set; } = string.Empty;
        public string GeminiApiKey  { get; set; } = string.Empty;
        public string OpenAiApiKey  { get; set; } = string.Empty;
        public string MistralApiKey { get; set; } = string.Empty;

        // ── Model selections (defaults match provider recommendations) ────────
        public string ClaudeModel  { get; set; } = "claude-sonnet-4-6";
        public string GeminiModel  { get; set; } = "models/gemini-2.5-flash";
        public string OpenAiModel  { get; set; } = "gpt-4o";
        public string MistralModel { get; set; } = "mistral-large-latest";

        // ── Task routing ──────────────────────────────────────────────────────
        public AiTaskAssignment TaskAssignment { get; set; } = new();
    }
}
