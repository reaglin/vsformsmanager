using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Static helpers shared by all <see cref="IFormParser"/> implementations.
    ///
    /// Handles namespace detection (both file-scoped C# 10 and block-scoped forms),
    /// class-name extraction, and <c>using</c>-directive collection.
    ///
    /// These are regex-based best-effort parsers suited to the predictable structure
    /// of Visual Studio–generated form files. They are not full C# parsers.
    /// </summary>
    internal static class ParserBase
    {
        // ── Compiled patterns ─────────────────────────────────────────────────

        // Matches both: "namespace Foo.Bar {" and "namespace Foo.Bar;"  (C# 10 file-scoped)
        private static readonly Regex NamespaceRegex = new(
            @"^\s*namespace\s+([\w.]+)\s*[;{]",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // Matches the first class declaration in the file.
        // Handles: public / internal / private / partial / sealed / abstract modifiers.
        private static readonly Regex ClassRegex = new(
            @"\bclass\s+(\w+)",
            RegexOptions.Compiled);

        // Matches class declarations that inherit from Form or UserControl,
        // handling fully-qualified names (System.Windows.Forms.Form) and short names.
        private static readonly Regex FormClassRegex = new(
            @"\bclass\s+(\w+)\s*:.*?\b(?:Form|UserControl)\b",
            RegexOptions.Compiled | RegexOptions.Singleline);

        // Matches: "using Some.Namespace;"   (not "using static" or "using X = Y")
        private static readonly Regex UsingRegex = new(
            @"^\s*using\s+(?!static\b)(?![\w.]+\s*=)([\w.]+)\s*;",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // ── Extraction methods ────────────────────────────────────────────────

        /// <summary>Returns the first namespace declaration found in <paramref name="content"/>.</summary>
        public static string ExtractNamespace(string content)
        {
            var m = NamespaceRegex.Match(content);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// Returns the name of the first class found in <paramref name="content"/>
        /// that inherits from <c>Form</c> or <c>UserControl</c>.
        /// Falls back to the first class name in the file if no such inheritance is found.
        /// </summary>
        public static string ExtractClassName(string content)
        {
            var m = FormClassRegex.Match(content);
            if (m.Success) return m.Groups[1].Value.Trim();

            m = ClassRegex.Match(content);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// Returns true when <paramref name="content"/> contains a class that explicitly
        /// inherits from <c>Form</c> or <c>UserControl</c>.
        /// Used by <see cref="FormParserFactory"/> to confirm code-only form files.
        /// </summary>
        public static bool IsFormClass(string content) =>
            FormClassRegex.IsMatch(content);

        /// <summary>
        /// Extracts all standard <c>using</c> directives from <paramref name="content"/>
        /// and returns them as <see cref="CodeDependency"/> instances, sorted by namespace.
        /// Skips <c>using static</c> and alias forms (<c>using X = Y</c>).
        /// </summary>
        public static List<CodeDependency> ExtractUsings(string content) =>
            UsingRegex.Matches(content)
                      .Cast<Match>()
                      .Select(m => new CodeDependency { Namespace = m.Groups[1].Value.Trim() })
                      .DistinctBy(d => d.Namespace, StringComparer.OrdinalIgnoreCase)
                      .OrderBy(d => d.IsFramework)   // project deps first
                      .ThenBy(d => d.Namespace, StringComparer.OrdinalIgnoreCase)
                      .ToList();

        // ── Type-name helper ──────────────────────────────────────────────────

        /// <summary>
        /// Returns just the unqualified type name from a fully-qualified name.
        /// E.g. <c>"System.Windows.Forms.Button"</c> → <c>"Button"</c>.
        /// </summary>
        public static string ShortTypeName(string fullType)
        {
            var dot = fullType.LastIndexOf('.');
            return dot >= 0 ? fullType[(dot + 1)..] : fullType;
        }
    }
}
