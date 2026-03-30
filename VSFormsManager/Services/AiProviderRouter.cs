using VSFormsManager.Models;
using VSFormsManager.Services.Providers;

namespace VSFormsManager.Services
{
    /// <summary>
    /// Resolves the correct <see cref="IAiProvider"/> for a given <see cref="AiTask"/>
    /// based on the current <see cref="AppSettings"/> task assignment.
    ///
    /// Usage:
    ///   var provider = AiProviderRouter.GetProvider(AiTask.CodeConversion, AppSession.Settings);
    ///   var result   = await provider.GenerateAsync(systemPrompt, userPrompt, ct);
    /// </summary>
    public static class AiProviderRouter
    {
        // ── Display names ─────────────────────────────────────────────────────

        private static readonly Dictionary<AiProviderType, string> ProviderDisplayNames = new()
        {
            { AiProviderType.Claude,  "Claude (Anthropic)"  },
            { AiProviderType.Gemini,  "Gemini (Google)"     },
            { AiProviderType.OpenAi,  "GPT-4o (OpenAI)"    },
            { AiProviderType.Mistral, "Mistral AI"          }
        };

        // ── Default models ────────────────────────────────────────────────────

        public static readonly Dictionary<AiProviderType, string> DefaultModels = new()
        {
            { AiProviderType.Claude,  "claude-sonnet-4-6"       },
            { AiProviderType.Gemini,  "models/gemini-2.5-flash" },
            { AiProviderType.OpenAi,  "gpt-4o"                  },
            { AiProviderType.Mistral, "mistral-large-latest"    }
        };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves and constructs the <see cref="IAiProvider"/> assigned to
        /// <paramref name="task"/>. Throws if the resolved provider has no API key.
        /// </summary>
        public static IAiProvider GetProvider(AiTask task, AppSettings settings)
        {
            var providerType = settings.TaskAssignment.GetProvider(task);
            return BuildProvider(providerType, settings);
        }

        /// <summary>
        /// Returns the display name for the provider assigned to <paramref name="task"/>.
        /// </summary>
        public static string GetProviderDisplayName(AiTask task, AppSettings settings)
        {
            var pt = settings.TaskAssignment.GetProvider(task);
            return ProviderDisplayNames.TryGetValue(pt, out var name) ? name : pt.ToString();
        }

        /// <summary>
        /// Returns true when the provider assigned to <paramref name="task"/> has an API key set.
        /// </summary>
        public static bool TaskHasKey(AiTask task, AppSettings settings)
            => ProviderHasKey(settings.TaskAssignment.GetProvider(task), settings);

        /// <summary>
        /// Returns true when <paramref name="provider"/> has a non-empty API key in settings.
        /// </summary>
        public static bool ProviderHasKey(AiProviderType provider, AppSettings settings) =>
            provider switch
            {
                AiProviderType.Claude  => !string.IsNullOrWhiteSpace(settings.ClaudeApiKey),
                AiProviderType.Gemini  => !string.IsNullOrWhiteSpace(settings.GeminiApiKey),
                AiProviderType.OpenAi  => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey),
                AiProviderType.Mistral => !string.IsNullOrWhiteSpace(settings.MistralApiKey),
                _                      => false
            };

        /// <summary>
        /// Returns all providers that currently have an API key configured.
        /// Use to populate task-assignment dropdowns.
        /// </summary>
        public static List<AiProviderType> GetAvailableProviders(AppSettings settings) =>
            Enum.GetValues<AiProviderType>()
                .Where(p => p != AiProviderType.None && ProviderHasKey(p, settings))
                .ToList();

        /// <summary>All provider types except <see cref="AiProviderType.None"/>.</summary>
        public static IEnumerable<AiProviderType> AllProviders =>
            Enum.GetValues<AiProviderType>().Where(p => p != AiProviderType.None);

        /// <summary>Human-readable display name for a provider type.</summary>
        public static string DisplayName(AiProviderType provider) =>
            ProviderDisplayNames.TryGetValue(provider, out var name) ? name : provider.ToString();

        // ── Internal builder ──────────────────────────────────────────────────

        private static IAiProvider BuildProvider(AiProviderType type, AppSettings settings) =>
            type switch
            {
                AiProviderType.Claude  => new ClaudeProvider(
                                             settings.ClaudeApiKey,
                                             settings.ClaudeModel),

                AiProviderType.Gemini  => new GeminiProvider(
                                             settings.GeminiApiKey,
                                             settings.GeminiModel),

                AiProviderType.OpenAi  => new OpenAiProvider(
                                             settings.OpenAiApiKey,
                                             settings.OpenAiModel),

                AiProviderType.Mistral => new MistralProvider(
                                             settings.MistralApiKey,
                                             settings.MistralModel),

                _ => throw new InvalidOperationException($"Unknown provider type: {type}")
            };
    }
}
