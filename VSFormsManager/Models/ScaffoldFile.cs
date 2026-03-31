namespace VSFormsManager.Models
{
    /// <summary>
    /// Represents a single file that will be written into the scaffolded project.
    /// Carries the relative path within the new project tree, the file content
    /// (optionally namespace-rewritten), the original source path for reference,
    /// and a category used to group files in the review tree view.
    /// </summary>
    public class ScaffoldFile
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>
        /// Path relative to the new project root, using the OS directory separator.
        /// e.g. <c>Services\Providers\ClaudeProvider.cs</c>
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>The complete file content that will be written to disk.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Absolute path of the file in the source project, if it came from one.
        /// Empty for generated files (Program.cs, .csproj, .sln).
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        // ── Categorisation ────────────────────────────────────────────────────

        /// <summary>Why this file was included — used to group entries in the review view.</summary>
        public ScaffoldFileCategory Category { get; set; } = ScaffoldFileCategory.Other;

        // ── User intent ───────────────────────────────────────────────────────

        /// <summary>
        /// Whether this file should actually be written.
        /// The user can uncheck individual files in the review step.
        /// Always true for the .csproj and .sln files.
        /// </summary>
        public bool IsIncluded { get; set; } = true;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>File name without directory (for display).</summary>
        public string FileName => Path.GetFileName(RelativePath);

        /// <summary>Relative directory portion only (for tree grouping).</summary>
        public string RelativeDirectory =>
            Path.GetDirectoryName(RelativePath) ?? string.Empty;

        public override string ToString() => RelativePath;
    }

    /// <summary>Category labels for grouping files in the review wizard step.</summary>
    public enum ScaffoldFileCategory
    {
        /// <summary>A selected form file (.cs / .Designer.cs / .xaml / .xaml.cs).</summary>
        Form,

        /// <summary>A shared model class found under the Models folder.</summary>
        Model,

        /// <summary>A service or provider class found under the Services folder.</summary>
        Service,

        /// <summary>A generated or infrastructure file (Program.cs, .csproj, .sln).</summary>
        Infrastructure,

        /// <summary>Any other source file pulled in via dependency scanning.</summary>
        Other
    }
}
