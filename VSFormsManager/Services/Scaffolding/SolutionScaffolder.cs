using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Scaffolding
{
    /// <summary>
    /// Result returned by <see cref="SolutionScaffolder.ScaffoldAsync"/>.
    /// </summary>
    public class ScaffoldResult
    {
        public bool         Success       { get; set; }
        public string?      ErrorMessage  { get; set; }
        public List<string> WrittenPaths  { get; set; } = new();
        public List<string> SkippedPaths  { get; set; } = new();
        public string       SolutionPath  { get; set; } = string.Empty;

        public static ScaffoldResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// Orchestrates the full project scaffold operation:
    ///
    ///   1. Optionally rewrite root namespaces in every copied file.
    ///   2. Create the solution and project directory trees.
    ///   3. Write every <see cref="ScaffoldFile"/> whose
    ///      <see cref="ScaffoldFile.IsIncluded"/> is true.
    ///   4. Generate and write the <c>.csproj</c> and <c>.sln</c> files.
    ///
    /// Runs on a background thread via <c>Task.Run</c> internally;
    /// the caller awaits <see cref="ScaffoldAsync"/>.
    /// </summary>
    public static class SolutionScaffolder
    {
        /// <summary>
        /// Performs the scaffold operation described by <paramref name="config"/>
        /// using the file list in <paramref name="files"/>.
        ///
        /// Reports progress messages via <paramref name="progress"/>.
        /// </summary>
        public static async Task<ScaffoldResult> ScaffoldAsync(
            ProjectScaffoldConfig      config,
            IReadOnlyList<ScaffoldFile> files,
            CsprojReader.CsprojInfo    sourceInfo,
            IProgress<string>?         progress,
            CancellationToken          cancellationToken)
        {
            return await Task.Run(() =>
                Scaffold(config, files, sourceInfo, progress, cancellationToken),
                cancellationToken);
        }

        // ── Core (runs on thread-pool thread) ─────────────────────────────────

        private static ScaffoldResult Scaffold(
            ProjectScaffoldConfig      config,
            IReadOnlyList<ScaffoldFile> files,
            CsprojReader.CsprojInfo    sourceInfo,
            IProgress<string>?         progress,
            CancellationToken          ct)
        {
            var result = new ScaffoldResult();

            try
            {
                ct.ThrowIfCancellationRequested();

                // ── Validate output path ──────────────────────────────────────
                if (Directory.Exists(config.SolutionDirectory))
                    return ScaffoldResult.Fail(
                        $"The output folder already exists:\r\n{config.SolutionDirectory}\r\n\r\n" +
                        "Choose a different solution name or parent directory.");

                // ── Create directory structure ────────────────────────────────
                progress?.Report($"Creating directory: {config.SolutionDirectory}");
                Directory.CreateDirectory(config.ProjectDirectory);

                // ── Write source files ────────────────────────────────────────
                var included = files.Where(f => f.IsIncluded).ToList();
                int i = 0;
                foreach (var file in included)
                {
                    ct.ThrowIfCancellationRequested();
                    i++;
                    progress?.Report(
                        $"Writing file {i}/{included.Count}: {file.RelativePath}");

                    var destPath = Path.Combine(config.ProjectDirectory, file.RelativePath);
                    var destDir  = Path.GetDirectoryName(destPath)!;
                    Directory.CreateDirectory(destDir);

                    var content = config.RewriteNamespaces
                        ? RewriteNamespace(file.Content,
                                           config.SourceRootNamespace,
                                           config.RootNamespace)
                        : file.Content;

                    File.WriteAllText(destPath, content);
                    result.WrittenPaths.Add(destPath);
                }

                foreach (var file in files.Where(f => !f.IsIncluded))
                    result.SkippedPaths.Add(file.RelativePath);

                // ── Generate .csproj ──────────────────────────────────────────
                ct.ThrowIfCancellationRequested();
                progress?.Report("Generating .csproj…");

                var csprojContent = CsprojGenerator.Generate(config, sourceInfo);
                var csprojPath    = Path.Combine(config.ProjectDirectory,
                                                  $"{config.SolutionName}.csproj");
                File.WriteAllText(csprojPath, csprojContent);
                result.WrittenPaths.Add(csprojPath);

                // ── Generate .sln ─────────────────────────────────────────────
                ct.ThrowIfCancellationRequested();
                progress?.Report("Generating .sln…");

                var slnContent = SlnGenerator.Generate(config.SolutionName);
                var slnPath    = Path.Combine(config.SolutionDirectory,
                                               $"{config.SolutionName}.sln");
                File.WriteAllText(slnPath, slnContent);
                result.WrittenPaths.Add(slnPath);
                result.SolutionPath = slnPath;

                // ── Done ──────────────────────────────────────────────────────
                result.Success = true;
                progress?.Report("Done.");
            }
            catch (OperationCanceledException)
            {
                return ScaffoldResult.Fail("Operation was cancelled.");
            }
            catch (Exception ex)
            {
                return ScaffoldResult.Fail(
                    $"Scaffolding failed: {ex.GetType().Name} — {ex.Message}");
            }

            return result;
        }

        // ── Namespace rewrite ─────────────────────────────────────────────────

        /// <summary>
        /// Replaces all occurrences of <paramref name="oldRoot"/> with
        /// <paramref name="newRoot"/> in C# namespace declarations and
        /// using directives. Uses word-boundary anchors to avoid partial matches.
        /// </summary>
        private static string RewriteNamespace(
            string content, string oldRoot, string newRoot)
        {
            if (string.IsNullOrEmpty(oldRoot) ||
                string.IsNullOrEmpty(newRoot) ||
                oldRoot.Equals(newRoot, StringComparison.Ordinal))
                return content;

            // Match the root only when followed by . or end of identifier
            // to avoid replacing 'OldRoot' inside 'OldRootExtra'
            var pattern     = $@"\b{Regex.Escape(oldRoot)}(?=\.|;|\s)";
            return Regex.Replace(content, pattern, newRoot);
        }
    }
}
