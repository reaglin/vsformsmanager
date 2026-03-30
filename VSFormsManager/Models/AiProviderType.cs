namespace VSFormsManager.Models
{
    /// <summary>
    /// Identifies a supported AI provider. Used as the key for API-key storage
    /// and task-to-provider routing in <see cref="AiTaskAssignment"/>.
    /// </summary>
    public enum AiProviderType
    {
        None    = 0,
        Claude  = 1,   // Anthropic Claude
        Gemini  = 2,   // Google Gemini
        OpenAi  = 3,   // OpenAI GPT-4o
        Mistral = 4    // Mistral AI
    }

    /// <summary>
    /// The AI-driven tasks used in the VSFormsManager pipeline.
    /// Each task can be independently routed to any configured provider.
    /// </summary>
    public enum AiTask
    {
        /// <summary>Analyse an existing Visual Studio form and extract its structure.</summary>
        FormAnalysis   = 0,

        /// <summary>Convert form code from one format to another (e.g. Designer → code-only → XAML).</summary>
        CodeConversion = 1
    }
}
