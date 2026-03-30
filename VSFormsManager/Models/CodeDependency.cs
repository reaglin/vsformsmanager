using System.Text.Json.Serialization;

namespace VSFormsManager.Models
{
    /// <summary>
    /// A namespace dependency extracted from a <c>using</c> directive in a form's source file.
    /// </summary>
    public class CodeDependency
    {
        /// <summary>The full namespace string (e.g. <c>System.Windows.Forms</c>).</summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// True when this is a well-known framework namespace (System.* or Microsoft.*).
        /// Computed at runtime from <see cref="Namespace"/>; not stored in JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsFramework =>
            Namespace.StartsWith("System",    StringComparison.OrdinalIgnoreCase) ||
            Namespace.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase);

        /// <summary>"Framework" or "Project" — for display in the dependencies list.</summary>
        [JsonIgnore]
        public string Category => IsFramework ? "Framework" : "Project";

        public override string ToString() => Namespace;
    }
}
