namespace VSFormsManager.Models
{
    /// <summary>
    /// The complete record for a Visual Studio form that has been discovered and
    /// parsed by VSFormsManager. One record is stored per form in <c>forms.json</c>.
    /// </summary>
    public class FormRecord
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Stable unique identifier. Generated once when the record is first created.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // ── Form metadata ─────────────────────────────────────────────────────

        /// <summary>The C# class name of the form (e.g. <c>frmMain</c>).</summary>
        public string FormName { get; set; } = string.Empty;

        /// <summary>Fully-qualified C# namespace the form class lives in.</summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>The on-disk format of the form.</summary>
        public FormType FormType { get; set; } = FormType.Unknown;

        // ── File locations ────────────────────────────────────────────────────

        /// <summary>
        /// Absolute path to the primary source file.
        /// For WinForms Designer this is <c>MyForm.cs</c> (not the <c>.Designer.cs</c>).
        /// For XAML this is the <c>.xaml</c> file.
        /// Used as the unique key when updating records in the repository.
        /// </summary>
        public string FormFilePath { get; set; } = string.Empty;

        // ── Project ───────────────────────────────────────────────────────────

        /// <summary>Display name of the containing project (taken from the .csproj filename).</summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Absolute path to the .csproj file, or empty if not found.</summary>
        public string ProjectFilePath { get; set; } = string.Empty;

        // ── Parsed content ────────────────────────────────────────────────────

        /// <summary>Controls discovered on the form during the last parse.</summary>
        public List<ControlInfo> Controls { get; set; } = new();

        /// <summary>Namespace dependencies extracted from <c>using</c> directives.</summary>
        public List<CodeDependency> CodeDependencies { get; set; } = new();

        // ── Housekeeping ──────────────────────────────────────────────────────

        /// <summary>UTC timestamp of the most recent successful parse.</summary>
        public DateTime LastScanned { get; set; } = DateTime.UtcNow;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>True when the primary source file still exists on disk.</summary>
        public bool FileExists => File.Exists(FormFilePath);

        /// <summary>Directory containing the primary source file.</summary>
        public string FormDirectory => Path.GetDirectoryName(FormFilePath) ?? string.Empty;

        /// <summary>Human-readable form-type label for display.</summary>
        public string FormTypeLabel => FormType switch
        {
            FormType.WinFormsDesigner => "WinForms (Designer + Code)",
            FormType.WinFormsCodeOnly => "WinForms (Code Only)",
            FormType.Xaml             => "XAML",
            _                         => "Unknown"
        };

        public override string ToString() => $"{ProjectName} / {FormName}";
    }
}
