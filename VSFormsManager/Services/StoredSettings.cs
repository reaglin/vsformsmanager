using VSFormsManager.Models;

namespace VSFormsManager.Services
{
    /// <summary>
    /// The shape of the JSON settings file on disk.
    ///
    /// API keys are stored as DPAPI-encrypted Base64 blobs (via <see cref="EncryptionHelper"/>).
    /// All other fields (model names, task assignments) are plain text — they are not
    /// sensitive and encrypting them would make the file harder to inspect or hand-edit
    /// when troubleshooting.
    ///
    /// <see cref="SettingsManager"/> is the only class that reads or writes this type.
    /// The rest of the application works exclusively with <see cref="AppSettings"/>,
    /// which always holds decrypted, ready-to-use values.
    /// </summary>
    internal class StoredSettings
    {
        // ── Encrypted API keys (Base64 DPAPI blobs) ───────────────────────────
        public string ClaudeApiKeyEnc  { get; set; } = string.Empty;
        public string GeminiApiKeyEnc  { get; set; } = string.Empty;
        public string OpenAiApiKeyEnc  { get; set; } = string.Empty;
        public string MistralApiKeyEnc { get; set; } = string.Empty;

        // ── Plain-text model names ────────────────────────────────────────────
        public string ClaudeModel  { get; set; } = "claude-sonnet-4-6";
        public string GeminiModel  { get; set; } = "models/gemini-2.5-flash";
        public string OpenAiModel  { get; set; } = "gpt-4o";
        public string MistralModel { get; set; } = "mistral-large-latest";

        // ── Task assignment (plain text enum names) ───────────────────────────
        public AiTaskAssignment TaskAssignment { get; set; } = new();

        // ── Conversion helpers ────────────────────────────────────────────────

        /// <summary>
        /// Decrypts all API keys and returns a populated <see cref="AppSettings"/>
        /// ready for use by the application.
        /// </summary>
        public AppSettings ToAppSettings() => new()
        {
            ClaudeApiKey  = EncryptionHelper.Decrypt(ClaudeApiKeyEnc),
            GeminiApiKey  = EncryptionHelper.Decrypt(GeminiApiKeyEnc),
            OpenAiApiKey  = EncryptionHelper.Decrypt(OpenAiApiKeyEnc),
            MistralApiKey = EncryptionHelper.Decrypt(MistralApiKeyEnc),

            ClaudeModel  = ClaudeModel,
            GeminiModel  = GeminiModel,
            OpenAiModel  = OpenAiModel,
            MistralModel = MistralModel,

            TaskAssignment = TaskAssignment
        };

        /// <summary>
        /// Encrypts all API keys from <paramref name="settings"/> and returns a
        /// <see cref="StoredSettings"/> ready to serialise to disk.
        /// </summary>
        public static StoredSettings FromAppSettings(AppSettings settings) => new()
        {
            ClaudeApiKeyEnc  = EncryptionHelper.Encrypt(settings.ClaudeApiKey),
            GeminiApiKeyEnc  = EncryptionHelper.Encrypt(settings.GeminiApiKey),
            OpenAiApiKeyEnc  = EncryptionHelper.Encrypt(settings.OpenAiApiKey),
            MistralApiKeyEnc = EncryptionHelper.Encrypt(settings.MistralApiKey),

            ClaudeModel  = settings.ClaudeModel,
            GeminiModel  = settings.GeminiModel,
            OpenAiModel  = settings.OpenAiModel,
            MistralModel = settings.MistralModel,

            TaskAssignment = settings.TaskAssignment
        };
    }
}
