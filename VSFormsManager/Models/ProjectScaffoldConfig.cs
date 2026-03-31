namespace VSFormsManager.Models
{
    /// <summary>
    /// Collects every piece of configuration the user provides across the four
    /// wizard steps in <see cref="VSFormsManager.frmBatchProject"/>.
    ///
    /// Passed through the wizard steps and handed to
    /// <see cref="VSFormsManager.Services.Scaffolding.SolutionScaffolder"/> to
    /// generate the new solution on disk.
    /// </summary>
    public class ProjectScaffoldConfig
    {
        // ── Step 1 — Selected forms ───────────────────────────────────────────

        /// <summary>Forms selected by the user in step 1.</summary>
        public List<FormRecord> SelectedForms { get; set; } = new();

        // ── Step 2 — Project identity ─────────────────────────────────────────

        /// <summary>
        /// The name given to both the solution (.sln) and the single project inside it.
        /// Also used as the project folder name under <see cref="OutputParentDirectory"/>.
        /// </summary>
        public string SolutionName { get; set; } = string.Empty;

        /// <summary>
        /// The parent directory in which the solution folder will be created.
        /// e.g. <c>C:\Source</c> → solution will be at <c>C:\Source\MySolution\</c>
        /// </summary>
        public string OutputParentDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Root C# namespace for all generated and copied files.
        /// Defaults to <see cref="SolutionName"/> with spaces removed.
        /// </summary>
        public string RootNamespace { get; set; } = string.Empty;

        /// <summary>
        /// The form to set as the startup form in the generated Program.cs.
        /// Must be one of the forms in <see cref="SelectedForms"/>.
        /// </summary>
        public FormRecord? StartupForm { get; set; }

        /// <summary>
        /// When true, every occurrence of the source project's root namespace
        /// in copied file contents is replaced with <see cref="RootNamespace"/>.
        /// Best-effort — uses simple string replacement.
        /// </summary>
        public bool RewriteNamespaces { get; set; } = true;

        // ── Project type (read from source .csproj) ───────────────────────────

        /// <summary>e.g. <c>net8.0-windows</c></summary>
        public string TargetFramework { get; set; } = "net8.0-windows";

        /// <summary>e.g. <c>WinExe</c>, <c>Exe</c>, <c>Library</c></summary>
        public string OutputType { get; set; } = "WinExe";

        /// <summary>True when the source project uses Windows Forms.</summary>
        public bool UseWindowsForms { get; set; } = true;

        /// <summary>Source project's root namespace (detected from .csproj or form files).</summary>
        public string SourceRootNamespace { get; set; } = string.Empty;

        /// <summary>Absolute path to the source .csproj file.</summary>
        public string SourceProjectFilePath { get; set; } = string.Empty;

        // ── Step 3 — Package references ───────────────────────────────────────

        /// <summary>
        /// NuGet package references read from the source .csproj.
        /// Each entry can be toggled off by the user in the review step.
        /// </summary>
        public List<PackageReferenceEntry> PackageReferences { get; set; } = new();

        // ── Derived paths ─────────────────────────────────────────────────────

        /// <summary>Full path of the solution folder that will be created.</summary>
        public string SolutionDirectory =>
            Path.Combine(OutputParentDirectory, SolutionName);

        /// <summary>Full path to the project folder inside the solution folder.</summary>
        public string ProjectDirectory =>
            Path.Combine(SolutionDirectory, SolutionName);
    }

    /// <summary>A single NuGet PackageReference entry with a user-controllable include flag.</summary>
    public class PackageReferenceEntry
    {
        public string PackageId { get; set; } = string.Empty;
        public string Version   { get; set; } = string.Empty;
        public bool   IsIncluded { get; set; } = true;

        public override string ToString() => $"{PackageId} {Version}";
    }
}
