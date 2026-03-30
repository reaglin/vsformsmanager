using VSFormsManager.Models;

namespace VSFormsManager.Services.Conversion
{
    /// <summary>
    /// Builds the system prompt and user prompt sent to the AI for form conversion.
    ///
    /// Prompts are tailored to each source→target format pair to give the AI
    /// specific conversion guidance (e.g. WinForms control → WPF element mappings).
    ///
    /// Output format: the AI is instructed to wrap each output file in
    /// <c>&lt;file name="Filename.ext"&gt;...&lt;/file&gt;</c> tags so the caller
    /// can reliably parse multiple files from a single response.
    /// </summary>
    public static class ConversionPromptBuilder
    {
        // ── Format descriptions ───────────────────────────────────────────────

        private static string FormatDescription(FormType t) => t switch
        {
            FormType.WinFormsDesigner =>
                "Windows Forms with Visual Studio Designer " +
                "(two partial-class files: MyForm.cs for business logic " +
                "and MyForm.Designer.cs for InitializeComponent)",

            FormType.WinFormsCodeOnly =>
                "Windows Forms Code-Only " +
                "(single .cs file; no designer file — all control setup is in code)",

            FormType.Xaml =>
                "XAML / WPF (MyForm.xaml markup file + MyForm.xaml.cs code-behind)",

            _ => t.ToString()
        };

        // ── Expected output filenames ─────────────────────────────────────────

        public static IReadOnlyList<string> ExpectedOutputFiles(
            string baseName, FormType targetType) => targetType switch
        {
            FormType.WinFormsDesigner => new[] { $"{baseName}.cs", $"{baseName}.Designer.cs" },
            FormType.WinFormsCodeOnly => new[] { $"{baseName}.cs" },
            FormType.Xaml             => new[] { $"{baseName}.xaml", $"{baseName}.xaml.cs" },
            _                         => new[] { $"{baseName}.cs" }
        };

        // ── System prompt ─────────────────────────────────────────────────────

        public static string BuildSystemPrompt() => """
            You are an expert C# developer specialising in Visual Studio Windows Forms and WPF/XAML.
            Your task is to convert form source files between Visual Studio formats.

            OUTPUT RULES — follow exactly:
            1. Output ONLY the converted file contents. No markdown, no code-fence backticks,
               no explanatory text outside the <file> tags.
            2. Wrap EACH output file in:
                   <file name="ExactFileName.ext">
                   ...complete file content...
                   </file>
            3. Output COMPLETE files. Never truncate. Never use "..." or "// rest of code here".
            4. Preserve ALL business logic, event handlers, field declarations, and UI behaviour.
            5. Keep the original class name and namespace UNLESS the prompt specifies different ones.
            6. For commented-out dependency blocks, use this exact wrapper:
                   /* DEPENDENCY_START: Full.Namespace.Here */
                   ...affected using directive and any code referencing it...
                   /* DEPENDENCY_END: Full.Namespace.Here */
            """;

        // ── User prompt ───────────────────────────────────────────────────────

        public static string BuildUserPrompt(
            FormRecord              source,
            FormType                targetType,
            Dictionary<string, string> sourceFiles,
            string                  outputBaseName,
            string                  outputNamespace,
            IEnumerable<string>     namespacesToComment)
        {
            var commentList = namespacesToComment.ToList();
            var expected    = ExpectedOutputFiles(outputBaseName, targetType);
            var sb          = new System.Text.StringBuilder();

            // ── Header ────────────────────────────────────────────────────────
            sb.AppendLine($"Convert the following {FormatDescription(source.FormType)} form");
            sb.AppendLine($"to: {FormatDescription(targetType)}");
            sb.AppendLine();

            // ── Names ─────────────────────────────────────────────────────────
            if (!string.Equals(outputBaseName, source.FormName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(outputNamespace, source.Namespace, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("NAME / NAMESPACE CHANGES:");
                sb.AppendLine($"  Class name: {outputBaseName}");
                sb.AppendLine($"  Namespace:  {outputNamespace}");
                sb.AppendLine();
            }

            // ── Commented-out dependencies ────────────────────────────────────
            if (commentList.Count > 0)
            {
                sb.AppendLine("COMMENT OUT THESE NAMESPACES (unavailable in target project):");
                foreach (var ns in commentList)
                    sb.AppendLine($"  - {ns}");
                sb.AppendLine("Wrap each affected using directive and any referencing code in");
                sb.AppendLine("DEPENDENCY_START / DEPENDENCY_END comment blocks.");
                sb.AppendLine();
            }

            // ── Conversion-pair-specific notes ────────────────────────────────
            var notes = ConversionNotes(source.FormType, targetType, outputBaseName, outputNamespace);
            if (!string.IsNullOrEmpty(notes))
            {
                sb.AppendLine("CONVERSION NOTES:");
                sb.AppendLine(notes);
                sb.AppendLine();
            }

            // ── Expected output ───────────────────────────────────────────────
            sb.AppendLine("EXPECTED OUTPUT FILES:");
            foreach (var f in expected)
                sb.AppendLine($"  {f}");
            sb.AppendLine();

            // ── Source files ──────────────────────────────────────────────────
            sb.AppendLine("--- SOURCE FILES ---");
            sb.AppendLine();
            foreach (var (filename, content) in sourceFiles)
            {
                sb.AppendLine($"[FILE: {filename}]");
                sb.AppendLine(content);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ── Per-pair conversion notes ─────────────────────────────────────────

        private static string ConversionNotes(FormType source, FormType target,
            string outputBaseName = "FormName", string outputNamespace = "YourNamespace")
        {
            if (source == target) return string.Empty;

            return (source, target) switch
            {
                (FormType.WinFormsDesigner, FormType.WinFormsCodeOnly) => """
                    - Merge MyForm.cs and MyForm.Designer.cs into a single .cs file.
                    - Remove the 'partial' keyword; produce one complete non-partial class.
                    - Rename the merged InitializeComponent to remain private; call it from the constructor.
                    - Keep the Dispose method and the components field.
                    """,

                (FormType.WinFormsCodeOnly, FormType.WinFormsDesigner) => """
                    - Split into MyForm.cs (partial class, constructor calls InitializeComponent, business logic only)
                      and MyForm.Designer.cs (partial class, InitializeComponent, all control declarations/setup).
                    - MyForm.Designer.cs must follow Visual Studio designer-file conventions:
                        private System.ComponentModel.IContainer components = null;
                        protected override void Dispose(bool disposing) { ... }
                        private void InitializeComponent() { ... }
                        // private field declarations at the bottom
                    """,

                (FormType.WinFormsDesigner, FormType.Xaml) or
                (FormType.WinFormsCodeOnly, FormType.Xaml) => $"""
                    - Map WinForms controls to WPF equivalents:
                        Button→Button, TextBox→TextBox, Label→TextBlock,
                        ComboBox→ComboBox, ListBox→ListBox, CheckBox→CheckBox,
                        RadioButton→RadioButton, GroupBox→GroupBox,
                        Panel→StackPanel or Grid (choose based on layout),
                        TabControl→TabControl, TabPage→TabItem,
                        TreeView→TreeView, ListView→ListView,
                        DataGridView→DataGrid, PictureBox→Image,
                        ProgressBar→ProgressBar, StatusStrip→StatusBar,
                        MenuStrip→Menu, ToolStrip→ToolBar,
                        MultiLine TextBox→TextBox with AcceptsReturn="True" TextWrapping="Wrap"
                    - Root element: <Window> (or <UserControl> if source inherits UserControl).
                    - Keep all event handlers in the .xaml.cs code-behind.
                      Wire events via EventName="HandlerName" attributes in XAML.
                    - Use x:Name for controls referenced in code-behind.
                    - Include xmlns declarations:
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        x:Class="{outputNamespace}.{outputBaseName}"
                    """,

                (FormType.Xaml, FormType.WinFormsDesigner) => """
                    - Map WPF elements to WinForms controls (reverse of the WPF mapping above).
                    - x:Name attributes become private field names in the designer file.
                    - Produce MyForm.cs (partial, constructor, event handlers) and
                      MyForm.Designer.cs (InitializeComponent with all control creation and layout).
                    - XAML layout panels: map Grid/StackPanel to TableLayoutPanel/FlowLayoutPanel
                      or set manual Location/Size if the layout is simple.
                    """,

                (FormType.Xaml, FormType.WinFormsCodeOnly) => """
                    - Map WPF elements to WinForms controls.
                    - Produce a single .cs file containing the merged form.
                    - All control creation and configuration goes into a private InitializeComponent()
                      called from the constructor.
                    """,

                _ => string.Empty
            };
        }
    }
}
