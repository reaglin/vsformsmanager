namespace VSFormsManager.Services
{
    /// <summary>
    /// Contract that all AI provider implementations must satisfy.
    ///
    /// Each provider wraps its own REST API and authentication, presenting
    /// a uniform interface to the rest of the application. Services that need
    /// AI generation call through this interface rather than targeting a
    /// specific vendor API.
    ///
    /// Implementations live in <c>VSFormsManager.Services.Providers</c>.
    /// </summary>
    public interface IAiProvider
    {
        /// <summary>Human-readable provider name (e.g. "Claude (Anthropic)").</summary>
        string ProviderName { get; }

        /// <summary>
        /// Sends a prompt pair and returns the text response.
        ///
        /// <paramref name="systemPrompt"/> supplies instruction / role context.
        /// <paramref name="userPrompt"/>   is the main user message or payload.
        ///
        /// Returns the raw text of the first response candidate.
        /// Throws <see cref="InvalidOperationException"/> on API or key errors.
        /// </summary>
        Task<string> GenerateAsync(
            string            systemPrompt,
            string            userPrompt,
            CancellationToken cancellationToken);

        /// <summary>
        /// True when a non-empty API key has been supplied.
        /// Does not validate the key against the live API.
        /// </summary>
        bool HasApiKey { get; }
    }
}
