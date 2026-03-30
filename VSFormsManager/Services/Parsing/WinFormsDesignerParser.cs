using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Parses a classic Windows Forms form that uses the Visual Studio designer:
    /// a <c>MyForm.cs</c> partial-class file paired with a <c>MyForm.Designer.cs</c>
    /// generated file.
    ///
    /// Control discovery reads the <c>InitializeComponent</c> method in the designer
    /// file, which always contains explicit <c>this.control = new Type()</c>
    /// instantiations for every control on the form.
    ///
    /// Code dependencies are extracted from <c>using</c> directives in <c>MyForm.cs</c>.
    /// </summary>
    public class WinFormsDesignerParser : IFormParser
    {
        // Matches: this.controlName = new Some.Type(
        // Captures: group 1 = field name, group 2 = fully-qualified type
        private static readonly Regex CtrlInstantiationRegex = new(
            @"this\.(\w+)\s*=\s*new\s+([\w.]+)\s*\(",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // Non-control types that appear in InitializeComponent but are not UI controls
        private static readonly HashSet<string> ExcludedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Container", "IContainer",
            "ColumnStyle", "RowStyle",
            "DataGridViewCellStyle"
        };

        // Field names to exclude regardless of type
        private static readonly HashSet<string> ExcludedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "components"
        };

        // ── IFormParser ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public FormRecord Parse(string primaryFilePath)
        {
            // Redirect if the user accidentally opened the Designer file
            primaryFilePath = NormalizePrimaryPath(primaryFilePath);

            if (!File.Exists(primaryFilePath))
                throw new FileNotFoundException("Form file not found.", primaryFilePath);

            var csContent = File.ReadAllText(primaryFilePath);

            var record = new FormRecord
            {
                FormFilePath = primaryFilePath,
                FormType     = FormType.WinFormsDesigner,
                LastScanned  = DateTime.UtcNow,
                FormName     = ParserBase.ExtractClassName(csContent),
                Namespace    = ParserBase.ExtractNamespace(csContent),
                CodeDependencies = ParserBase.ExtractUsings(csContent)
            };

            // Parse the companion designer file for controls
            var designerPath = BuildDesignerPath(primaryFilePath);
            if (File.Exists(designerPath))
            {
                var designerContent = File.ReadAllText(designerPath);
                record.Controls = ExtractControls(designerContent);
            }

            // Locate the owning project
            var (projectName, projectPath) = ProjectLocator.FindProject(primaryFilePath);
            record.ProjectName    = projectName;
            record.ProjectFilePath = projectPath;

            return record;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures the path points to the main <c>.cs</c> file, not the designer file.
        /// E.g. <c>MyForm.Designer.cs</c> → <c>MyForm.cs</c>
        /// </summary>
        private static string NormalizePrimaryPath(string path)
        {
            if (path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = path[..^".Designer.cs".Length] + ".cs";
                if (File.Exists(candidate)) return candidate;
            }
            return path;
        }

        /// <summary>
        /// Derives the <c>.Designer.cs</c> path from the primary <c>.cs</c> path.
        /// E.g. <c>C:\Foo\MyForm.cs</c> → <c>C:\Foo\MyForm.Designer.cs</c>
        /// </summary>
        private static string BuildDesignerPath(string primaryPath)
        {
            // Strip .cs, append .Designer.cs
            var withoutExt = Path.ChangeExtension(primaryPath, null);   // removes ".cs"
            return withoutExt + ".Designer.cs";
        }

        /// <summary>
        /// Scans <c>InitializeComponent</c> in the designer file for all
        /// <c>this.xxx = new Type()</c> instantiations.
        /// </summary>
        private static List<ControlInfo> ExtractControls(string designerContent)
        {
            var controls = new List<ControlInfo>();
            var seen     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in CtrlInstantiationRegex.Matches(designerContent))
            {
                var name      = m.Groups[1].Value;
                var fullType  = m.Groups[2].Value;
                var shortType = ParserBase.ShortTypeName(fullType);

                if (ExcludedNames.Contains(name))    continue;
                if (ExcludedTypes.Contains(shortType)) continue;
                if (!seen.Add(name))                 continue;   // deduplicate

                controls.Add(new ControlInfo { Name = name, ControlType = shortType });
            }

            return controls.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
