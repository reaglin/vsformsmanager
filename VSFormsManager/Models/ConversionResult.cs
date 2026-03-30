namespace VSFormsManager.Models
{
    /// <summary>
    /// The result returned by <see cref="VSFormsManager.Services.Conversion.AiFormConverter"/>
    /// after attempting to convert a form from one format to another.
    /// </summary>
    public class ConversionResult
    {
        // ── Outcome ───────────────────────────────────────────────────────────

        /// <summary>True when the conversion completed and files were written successfully.</summary>
        public bool Success { get; set; }

        /// <summary>Error description when <see cref="Success"/> is false.</summary>
        public string? ErrorMessage { get; set; }

        // ── Output ────────────────────────────────────────────────────────────

        /// <summary>
        /// The files produced by the conversion.
        /// Key = filename (no path), Value = complete file content.
        /// Empty when <see cref="Success"/> is false.
        /// </summary>
        public Dictionary<string, string> OutputFiles { get; set; } = new();

        /// <summary>
        /// Full paths of files that were written to disk.
        /// Populated after <see cref="OutputFiles"/> have been saved.
        /// </summary>
        public List<string> WrittenPaths { get; set; } = new();

        // ── Advisory ─────────────────────────────────────────────────────────

        /// <summary>
        /// Non-fatal warnings produced during conversion (e.g. controls that
        /// have no direct equivalent in the target framework).
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Namespaces that were commented out in the output as requested.
        /// </summary>
        public List<string> CommentedOutNamespaces { get; set; } = new();

        // ── Debug ─────────────────────────────────────────────────────────────

        /// <summary>The raw text returned by the AI (kept for diagnostics).</summary>
        public string RawAiResponse { get; set; } = string.Empty;

        // ── Factories ─────────────────────────────────────────────────────────

        public static ConversionResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}
