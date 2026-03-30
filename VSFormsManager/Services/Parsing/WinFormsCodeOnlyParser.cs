using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Parses a Windows Forms form written entirely in a single <c>.cs</c> file
    /// with no companion <c>.Designer.cs</c> — i.e. the form creates and configures
    /// all its controls in code.
    ///
    /// Because there is no generated designer structure to rely on, control discovery
    /// works by scanning private field declarations for known WinForms control types.
    /// Fields using fully-qualified type names (e.g. <c>System.Windows.Forms.Button</c>)
    /// are also matched.
    ///
    /// Note: controls created only inside method bodies (not as fields) will not be
    /// detected — this is a known limitation of the regex approach.
    /// </summary>
    public class WinFormsCodeOnlyParser : IFormParser
    {
        // Comprehensive list of WinForms control short type names
        private static readonly HashSet<string> KnownControlTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Button", "TextBox", "Label", "ComboBox", "ListBox",
                "CheckBox", "RadioButton", "GroupBox", "Panel", "TabControl",
                "TabPage", "TreeView", "ListView", "DataGridView", "PictureBox",
                "ProgressBar", "TrackBar", "NumericUpDown", "DateTimePicker",
                "MonthCalendar", "RichTextBox", "MaskedTextBox", "LinkLabel",
                "CheckedListBox", "FlowLayoutPanel", "TableLayoutPanel",
                "SplitContainer", "Splitter", "StatusStrip", "MenuStrip",
                "ToolStrip", "ContextMenuStrip", "ToolStripMenuItem",
                "ToolStripButton", "ToolStripLabel", "ToolStripComboBox",
                "ToolStripTextBox", "ToolStripSeparator",
                "StatusBar", "ToolBar", "MainMenu", "MenuItem",
                "NotifyIcon", "Timer", "ErrorProvider", "ToolTip", "HelpProvider",
                "ImageList", "OpenFileDialog", "SaveFileDialog",
                "FolderBrowserDialog", "ColorDialog", "FontDialog",
                "PrintDialog", "PrintPreviewDialog",
                "WebBrowser", "PropertyGrid", "HScrollBar", "VScrollBar",
                "DomainUpDown", "Form", "UserControl", "ContainerControl"
            };

        // Matches: private [TypeName] [fieldName]; or private [TypeName] [fieldName] = ...;
        // Also matches fully-qualified: private System.Windows.Forms.Button btnOK;
        private static readonly Regex FieldRegex = new(
            @"private\s+([\w.]+(?:<[^>]+>)?)\s+(\w+)\s*(?:=.*?)?;",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // ── IFormParser ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public FormRecord Parse(string primaryFilePath)
        {
            if (!File.Exists(primaryFilePath))
                throw new FileNotFoundException("Form file not found.", primaryFilePath);

            var content = File.ReadAllText(primaryFilePath);

            if (!ParserBase.IsFormClass(content))
                throw new InvalidOperationException(
                    $"The file '{Path.GetFileName(primaryFilePath)}' does not appear to " +
                    "contain a class that inherits from Form or UserControl.");

            var record = new FormRecord
            {
                FormFilePath     = primaryFilePath,
                FormType         = FormType.WinFormsCodeOnly,
                LastScanned      = DateTime.UtcNow,
                FormName         = ParserBase.ExtractClassName(content),
                Namespace        = ParserBase.ExtractNamespace(content),
                CodeDependencies = ParserBase.ExtractUsings(content),
                Controls         = ExtractControls(content)
            };

            var (projectName, projectPath) = ProjectLocator.FindProject(primaryFilePath);
            record.ProjectName    = projectName;
            record.ProjectFilePath = projectPath;

            return record;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<ControlInfo> ExtractControls(string content)
        {
            var controls = new List<ControlInfo>();
            var seen     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in FieldRegex.Matches(content))
            {
                var fullType  = m.Groups[1].Value;
                var name      = m.Groups[2].Value;
                var shortType = ParserBase.ShortTypeName(fullType);

                if (!KnownControlTypes.Contains(shortType)) continue;
                if (!seen.Add(name)) continue;

                controls.Add(new ControlInfo { Name = name, ControlType = shortType });
            }

            return controls.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
