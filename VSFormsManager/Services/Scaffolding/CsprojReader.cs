using System.Xml.Linq;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Scaffolding
{
    /// <summary>
    /// Reads a Visual Studio <c>.csproj</c> file (SDK-style) and extracts:
    ///   • <c>TargetFramework</c>
    ///   • <c>OutputType</c>  (WinExe / Exe / Library)
    ///   • <c>UseWindowsForms</c> flag
    ///   • <c>Nullable</c>, <c>ImplicitUsings</c>
    ///   • All <c>PackageReference</c> items
    ///   • <c>RootNamespace</c> (falls back to project file name)
    ///
    /// Only SDK-style projects (Visual Studio 2017+) are supported.
    /// Classic non-SDK projects are detected and reported gracefully.
    /// </summary>
    public static class CsprojReader
    {
        /// <summary>Result of reading a .csproj file.</summary>
        public class CsprojInfo
        {
            public string TargetFramework   { get; set; } = "net8.0-windows";
            public string OutputType        { get; set; } = "WinExe";
            public bool   UseWindowsForms   { get; set; } = true;
            public bool   Nullable          { get; set; } = true;
            public bool   ImplicitUsings    { get; set; } = true;
            public string RootNamespace     { get; set; } = string.Empty;
            public bool   IsSdkStyle        { get; set; } = true;

            public List<PackageReferenceEntry> PackageReferences { get; set; } = new();
        }

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Reads <paramref name="csprojPath"/> and returns a <see cref="CsprojInfo"/>.
        /// Returns sensible defaults if the file cannot be parsed.
        /// </summary>
        public static CsprojInfo Read(string csprojPath)
        {
            var info = new CsprojInfo
            {
                RootNamespace = Path.GetFileNameWithoutExtension(csprojPath)
            };

            if (!File.Exists(csprojPath))
                return info;

            try
            {
                var xdoc = XDocument.Load(csprojPath);
                var root = xdoc.Root;
                if (root == null) return info;

                // Detect SDK-style by the presence of Sdk attribute or SDK Project element
                info.IsSdkStyle = root.Attribute("Sdk") != null
                    || root.Elements("PropertyGroup")
                           .Any(pg => pg.Elements().Any());

                // ── PropertyGroup values ──────────────────────────────────────
                foreach (var pg in root.Elements("PropertyGroup"))
                {
                    info.TargetFramework = GetIfPresent(pg, "TargetFramework", info.TargetFramework);
                    info.OutputType = GetIfPresent(pg, "OutputType", info.OutputType);
                    info.RootNamespace = GetIfPresent(pg, "RootNamespace", info.RootNamespace);
                    

                                        var uwf = pg.Element("UseWindowsForms")?.Value;
                    if (!string.IsNullOrEmpty(uwf))
                        info.UseWindowsForms = uwf.Equals("true", StringComparison.OrdinalIgnoreCase);

                    var nullable = pg.Element("Nullable")?.Value;
                    if (!string.IsNullOrEmpty(nullable))
                        info.Nullable = nullable.Equals("enable", StringComparison.OrdinalIgnoreCase);

                    var implUsings = pg.Element("ImplicitUsings")?.Value;
                    if (!string.IsNullOrEmpty(implUsings))
                        info.ImplicitUsings = implUsings.Equals("enable", StringComparison.OrdinalIgnoreCase);
                }

                // Fallback: if no RootNamespace element, use file name
                if (string.IsNullOrEmpty(info.RootNamespace))
                    info.RootNamespace = Path.GetFileNameWithoutExtension(csprojPath);

                // ── PackageReferences ─────────────────────────────────────────
                foreach (var ig in root.Elements("ItemGroup"))
                {
                    foreach (var pr in ig.Elements("PackageReference"))
                    {
                        var id      = pr.Attribute("Include")?.Value ?? string.Empty;
                        var version = pr.Attribute("Version")?.Value
                                   ?? pr.Element("Version")?.Value
                                   ?? string.Empty;

                        if (!string.IsNullOrEmpty(id))
                            info.PackageReferences.Add(new PackageReferenceEntry
                            {
                                PackageId   = id,
                                Version     = version,
                                IsIncluded  = true
                            });
                    }
                }
            }
            catch
            {
                // Return whatever we managed to populate
            }

            return info;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetIfPresent(ref string target, XElement parent, string elementName)
        {
            var val = parent.Element(elementName)?.Value;
            if (!string.IsNullOrWhiteSpace(val))
                target = val.Trim();
        }
        private static string GetIfPresent(XElement parent, string elementName, string currentValue)
        {
            var val = parent.Element(elementName)?.Value;
            return !string.IsNullOrWhiteSpace(val) ? val.Trim() : currentValue;
        }

    }
}
