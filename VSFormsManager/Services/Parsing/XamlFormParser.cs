using System.Xml.Linq;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Parses a WPF (or MAUI) XAML form consisting of a <c>.xaml</c> markup file
    /// and an optional <c>.xaml.cs</c> code-behind file.
    ///
    /// Accepts either file as the starting point — if the user opens the
    /// <c>.xaml.cs</c> it will be redirected to the <c>.xaml</c> file automatically.
    ///
    /// Control discovery finds all XAML elements that have an <c>x:Name</c> attribute
    /// (i.e. controls that are accessible from code-behind). The element's local tag
    /// name becomes the <see cref="ControlInfo.ControlType"/>.
    ///
    /// Namespace and form name are read from the root element's <c>x:Class</c> attribute.
    /// Code dependencies are extracted from <c>using</c> directives in the <c>.xaml.cs</c>.
    /// </summary>
    public class XamlFormParser : IFormParser
    {
        // Standard XAML namespace for x:Name, x:Class, etc.
        private static readonly XNamespace XamlNs =
            "http://schemas.microsoft.com/winfx/2006/xaml";

        // ── IFormParser ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public FormRecord Parse(string primaryFilePath)
        {
            var (xamlPath, csPath) = ResolvePaths(primaryFilePath);

            if (!File.Exists(xamlPath))
                throw new FileNotFoundException("XAML file not found.", xamlPath);

            var record = new FormRecord
            {
                FormFilePath = xamlPath,
                FormType     = FormType.Xaml,
                LastScanned  = DateTime.UtcNow
            };

            // ── Parse the XAML markup ─────────────────────────────────────────
            try
            {
                var xdoc = XDocument.Load(xamlPath);
                var root = xdoc.Root
                    ?? throw new InvalidOperationException("XAML file has no root element.");

                // x:Class="Namespace.ClassName"
                var xClass = root.Attribute(XamlNs + "Class")?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(xClass))
                {
                    var lastDot = xClass.LastIndexOf('.');
                    record.FormName  = lastDot >= 0 ? xClass[(lastDot + 1)..] : xClass;
                    record.Namespace = lastDot >= 0 ? xClass[..lastDot] : string.Empty;
                }

                // All descendant elements that carry x:Name are accessible controls
                record.Controls = root.Descendants()
                    .Select(e => new
                    {
                        Element = e,
                        Name    = e.Attribute(XamlNs + "Name")?.Value
                    })
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Select(e => new ControlInfo
                    {
                        Name        = e.Name!,
                        ControlType = e.Element.Name.LocalName
                    })
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                // XAML parse failed — populate what we can from the filename
                record.FormName = Path.GetFileNameWithoutExtension(
                                      Path.GetFileNameWithoutExtension(xamlPath)); // strip both .xaml and any prefix
            }

            // ── Parse the code-behind .xaml.cs ────────────────────────────────
            if (File.Exists(csPath))
            {
                var csContent = File.ReadAllText(csPath);
                record.CodeDependencies = ParserBase.ExtractUsings(csContent);

                // Fill in namespace / name from C# if XAML parse couldn't provide them
                if (string.IsNullOrEmpty(record.Namespace))
                    record.Namespace = ParserBase.ExtractNamespace(csContent);
                if (string.IsNullOrEmpty(record.FormName))
                    record.FormName = ParserBase.ExtractClassName(csContent);
            }

            // ── Locate the owning project ─────────────────────────────────────
            var (projectName, projectPath) = ProjectLocator.FindProject(xamlPath);
            record.ProjectName    = projectName;
            record.ProjectFilePath = projectPath;

            return record;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Given either a <c>.xaml</c> or <c>.xaml.cs</c> path, returns both
        /// the canonical XAML path and the expected code-behind path.
        /// </summary>
        private static (string XamlPath, string CsPath) ResolvePaths(string inputPath)
        {
            if (inputPath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                var xamlPath = inputPath[..^3];  // strip trailing ".cs"
                return (xamlPath, inputPath);
            }
            else
            {
                return (inputPath, inputPath + ".cs");
            }
        }
    }
}
