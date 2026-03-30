using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Inspects a source file and returns the appropriate <see cref="IFormParser"/>
    /// implementation along with the canonical primary file path to pass to it.
    ///
    /// Detection order:
    ///   1. <c>.xaml</c> or <c>.xaml.cs</c>   → <see cref="XamlFormParser"/>
    ///   2. <c>.cs</c> with a companion <c>.Designer.cs</c> → <see cref="WinFormsDesignerParser"/>
    ///   3. <c>.cs</c> whose content inherits from <c>Form</c>/<c>UserControl</c>
    ///                                          → <see cref="WinFormsCodeOnlyParser"/>
    ///   4. Anything else → <see cref="NotSupportedException"/>
    ///
    /// Usage:
    ///   var (parser, path) = FormParserFactory.GetParser(selectedFilePath);
    ///   var record         = parser.Parse(path);
    /// </summary>
    public static class FormParserFactory
    {
        /// <summary>
        /// Returns an <see cref="IFormParser"/> and the normalised primary file path
        /// for the form at <paramref name="filePath"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///   The file type is not recognised or not a form.
        /// </exception>
        public static (IFormParser Parser, string PrimaryPath) GetParser(string filePath)
        {
            filePath = Path.GetFullPath(filePath);  // normalise slashes / relative paths

            // ── XAML ──────────────────────────────────────────────────────────
            if (filePath.EndsWith(".xaml",    StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                return (new XamlFormParser(), filePath);
            }

            // ── C# files ──────────────────────────────────────────────────────
            if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                // Redirect .Designer.cs → main .cs so callers never need to worry about this
                if (filePath.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    var mainPath = filePath[..^".Designer.cs".Length] + ".cs";
                    if (File.Exists(mainPath))
                        filePath = mainPath;
                    // If the main file doesn't exist, continue with the designer file —
                    // WinFormsDesignerParser will handle it gracefully.
                }

                // Check for a companion designer file
                var designerPath = Path.ChangeExtension(filePath, null) + ".Designer.cs";
                if (File.Exists(designerPath))
                    return (new WinFormsDesignerParser(), filePath);

                // No designer file — check whether the content looks like a Form class
                var content = File.ReadAllText(filePath);
                if (ParserBase.IsFormClass(content))
                    return (new WinFormsCodeOnlyParser(), filePath);

                throw new NotSupportedException(
                    $"'{Path.GetFileName(filePath)}' does not appear to be a WinForms form " +
                    "(no .Designer.cs found and no Form/UserControl inheritance detected).");
            }

            throw new NotSupportedException(
                $"File type '{Path.GetExtension(filePath)}' is not supported. " +
                "Browse to a .cs or .xaml form file.");
        }

        /// <summary>
        /// Returns the <see cref="FormType"/> that would be assigned to <paramref name="filePath"/>
        /// without constructing a full parser. Useful for icon selection in the UI.
        /// </summary>
        public static FormType DetectFormType(string filePath)
        {
            if (filePath.EndsWith(".xaml",    StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
                return FormType.Xaml;

            if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var designerPath = Path.ChangeExtension(filePath, null) + ".Designer.cs";
                return File.Exists(designerPath)
                    ? FormType.WinFormsDesigner
                    : FormType.WinFormsCodeOnly;
            }

            return FormType.Unknown;
        }
    }
}
