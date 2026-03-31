using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Scaffolding
{
    /// <summary>
    /// Collects every source file needed to compile the selected forms in a new project.
    ///
    /// Algorithm:
    ///   1. Gather all .cs (and .xaml / .xaml.cs) files for each selected form.
    ///   2. Read <c>using</c> directives from those files.
    ///   3. For each project-specific namespace (not System.* / Microsoft.*),
    ///      derive the expected sub-folder under the source project root using
    ///      the convention: strip the root namespace prefix, convert '.' to separator.
    ///   4. Recursively collect all .cs files from those sub-folders.
    ///   5. Always include the entire <c>Models\</c> and <c>Services\</c> trees
    ///      if they exist, since these are the shared infrastructure layers.
    ///   6. Generate a <c>Program.cs</c> for the new project.
    ///
    /// All returned <see cref="ScaffoldFile"/> paths are relative to the project
    /// root and use the OS directory separator.
    /// </summary>
    public static class DependencyScanner
    {
        // Well-known top-level source folders to always include wholesale
        private static readonly string[] AlwaysIncludeFolders =
            { "Models", "Services", "Controls" };

        // Namespaces whose root means "pull in the whole folder tree"
        private static readonly string[] FrameworkPrefixes =
            { "System", "Microsoft", "Windows", "Newtonsoft" };

        private static readonly Regex UsingRegex = new(
            @"^\s*using\s+(?!static\b)(?![\w.]+\s*=)([\w.]+)\s*;",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Scans the source project rooted at the directory of
        /// <paramref name="sourceProjectFile"/> and returns a list of
        /// <see cref="ScaffoldFile"/> objects (not yet namespace-rewritten).
        ///
        /// Also appends a generated <c>Program.cs</c> when
        /// <paramref name="startupFormName"/> is provided.
        /// </summary>
        public static List<ScaffoldFile> Scan(
            IEnumerable<FormRecord> selectedForms,
            string                 sourceProjectFile,
            string?                startupFormName = null)
        {
            var projectRoot = Path.GetDirectoryName(sourceProjectFile) ?? string.Empty;
            var files       = new Dictionary<string, ScaffoldFile>(
                                  StringComparer.OrdinalIgnoreCase);

            // ── 1. Form files ─────────────────────────────────────────────────
            foreach (var form in selectedForms)
                CollectFormFiles(form, projectRoot, files);

            // ── 2. Always-include folder trees ────────────────────────────────
            foreach (var folder in AlwaysIncludeFolders)
            {
                var fullFolder = Path.Combine(projectRoot, folder);
                if (Directory.Exists(fullFolder))
                    CollectFolderFiles(fullFolder, projectRoot, files,
                                       FolderCategory(folder));
            }

            // ── 3. Additional folders derived from project-specific usings ────
            var allUsings = CollectAllUsings(files.Values);
            var rootNs    = DeriveRootNamespace(projectRoot);

            foreach (var ns in allUsings)
            {
                if (IsFrameworkNamespace(ns)) continue;

                var folder = NamespaceToRelativeFolder(ns, rootNs);
                if (string.IsNullOrEmpty(folder)) continue;

                var fullPath = Path.Combine(projectRoot, folder);
                if (Directory.Exists(fullPath))
                    CollectFolderFiles(fullPath, projectRoot, files,
                                       ScaffoldFileCategory.Other);
            }

            // ── 4. Generated Program.cs ───────────────────────────────────────
            if (!string.IsNullOrEmpty(startupFormName))
            {
                var programCs = GenerateProgramCs(startupFormName,
                                    DeriveRootNamespace(projectRoot));
                files["Program.cs"] = new ScaffoldFile
                {
                    RelativePath = "Program.cs",
                    Content      = programCs,
                    SourcePath   = string.Empty,
                    Category     = ScaffoldFileCategory.Infrastructure,
                    IsIncluded   = true
                };
            }

            return files.Values.OrderBy(f => f.RelativePath).ToList();
        }

        // ── Form file collection ──────────────────────────────────────────────

        private static void CollectFormFiles(
            FormRecord record, string projectRoot,
            Dictionary<string, ScaffoldFile> files)
        {
            AddFileIfExists(record.FormFilePath, projectRoot, files,
                            ScaffoldFileCategory.Form);

            // Companion files
            switch (record.FormType)
            {
                case FormType.WinFormsDesigner:
                    var designerPath =
                        Path.ChangeExtension(record.FormFilePath, null) + ".Designer.cs";
                    AddFileIfExists(designerPath, projectRoot, files,
                                    ScaffoldFileCategory.Form);
                    break;

                case FormType.Xaml:
                    AddFileIfExists(record.FormFilePath + ".cs", projectRoot, files,
                                    ScaffoldFileCategory.Form);
                    break;
            }
        }

        private static void AddFileIfExists(
            string absolutePath, string projectRoot,
            Dictionary<string, ScaffoldFile> files,
            ScaffoldFileCategory category)
        {
            if (!File.Exists(absolutePath)) return;

            var relative = MakeRelative(absolutePath, projectRoot);
            if (files.ContainsKey(relative)) return;

            files[relative] = new ScaffoldFile
            {
                RelativePath = relative,
                Content      = File.ReadAllText(absolutePath),
                SourcePath   = absolutePath,
                Category     = category,
                IsIncluded   = true
            };
        }

        // ── Folder collection ─────────────────────────────────────────────────

        private static void CollectFolderFiles(
            string absoluteFolder, string projectRoot,
            Dictionary<string, ScaffoldFile> files,
            ScaffoldFileCategory category)
        {
            foreach (var file in Directory.GetFiles(
                absoluteFolder, "*.cs", SearchOption.AllDirectories))
            {
                AddFileIfExists(file, projectRoot, files, category);
            }
            // XAML files in the folder
            foreach (var file in Directory.GetFiles(
                absoluteFolder, "*.xaml", SearchOption.AllDirectories))
            {
                AddFileIfExists(file, projectRoot, files, category);
                AddFileIfExists(file + ".cs", projectRoot, files, category);
            }
        }

        // ── Using extraction ──────────────────────────────────────────────────

        private static HashSet<string> CollectAllUsings(IEnumerable<ScaffoldFile> files)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
                foreach (Match m in UsingRegex.Matches(f.Content))
                    result.Add(m.Groups[1].Value.Trim());
            return result;
        }

        // ── Namespace helpers ─────────────────────────────────────────────────

        private static bool IsFrameworkNamespace(string ns) =>
            FrameworkPrefixes.Any(p =>
                ns.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Converts a namespace like <c>MyApp.Services.Providers</c> to a relative
        /// folder path like <c>Services\Providers</c> by stripping <paramref name="rootNs"/>.
        /// Returns empty string if the namespace does not start with <paramref name="rootNs"/>.
        /// </summary>
        private static string NamespaceToRelativeFolder(string ns, string rootNs)
        {
            if (string.IsNullOrEmpty(rootNs)) return string.Empty;

            var prefix = rootNs + ".";
            if (!ns.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            var relative = ns[prefix.Length..];
            return relative.Replace('.', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Best-effort: derive the root namespace from the project directory by
        /// looking for the first C# file and reading its namespace declaration.
        /// Falls back to the directory name.
        /// </summary>
        private static string DeriveRootNamespace(string projectRoot)
        {
            var firstCs = Directory.GetFiles(projectRoot, "*.cs",
                SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (firstCs != null)
            {
                var content = File.ReadAllText(firstCs);
                var m = Regex.Match(content,
                    @"^\s*namespace\s+([\w.]+)\s*[;{]",
                    RegexOptions.Multiline);
                if (m.Success)
                {
                    var ns = m.Groups[1].Value;
                    // Take only the root segment (first component)
                    var root = ns.Split('.')[0];
                    return root;
                }
            }

            return Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar));
        }

        // ── Program.cs generation ─────────────────────────────────────────────

        private static string GenerateProgramCs(string startupFormName, string rootNamespace)
        {
            var nl = Environment.NewLine;
            return
                $"namespace {rootNamespace}{nl}" +
                $"{{{nl}" +
                $"    internal static class Program{nl}" +
                $"    {{{nl}" +
                $"        /// <summary>{nl}" +
                $"        ///  The main entry point for the application.{nl}" +
                $"        /// </summary>{nl}" +
                $"        [STAThread]{nl}" +
                $"        static void Main(){nl}" +
                $"        {{{nl}" +
                $"            ApplicationConfiguration.Initialize();{nl}" +
                $"            Application.Run(new {startupFormName}());{nl}" +
                $"        }}{nl}" +
                $"    }}{nl}" +
                $"}}";
        }

        // ── Path helpers ──────────────────────────────────────────────────────

        private static string MakeRelative(string absolutePath, string projectRoot)
        {
            var root = projectRoot.TrimEnd(Path.DirectorySeparatorChar,
                                            Path.AltDirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;

            if (absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return absolutePath[root.Length..];

            return Path.GetFileName(absolutePath);
        }

        private static ScaffoldFileCategory FolderCategory(string folderName) =>
            folderName.ToLowerInvariant() switch
            {
                "models"   => ScaffoldFileCategory.Model,
                "services" => ScaffoldFileCategory.Service,
                "controls" => ScaffoldFileCategory.Other,
                _          => ScaffoldFileCategory.Other
            };
    }
}
