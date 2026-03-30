using System.Text.RegularExpressions;
using VSFormsManager.Models;

namespace VSFormsManager.Services.Conversion
{
    /// <summary>
    /// Converts a Visual Studio form between formats by calling the configured
    /// AI provider (routed through <see cref="AiProviderRouter"/>).
    ///
    /// Process:
    ///   1. Read source files from disk (all files that make up the source form).
    ///   2. Build a system prompt + user prompt via <see cref="ConversionPromptBuilder"/>.
    ///   3. Call the AI provider assigned to <see cref="AiTask.CodeConversion"/>.
    ///   4. Parse the AI response — extract each <c>&lt;file name="…"&gt;</c> block.
    ///   5. Write each output file to <paramref name="outputDirectory"/>.
    ///   6. Return a <see cref="ConversionResult"/> describing success/failure.
    ///
    /// Note: AI providers currently cap responses at 8 000 tokens. Very large forms
    /// (thousands of lines) may be truncated. Split large forms before converting.
    /// </summary>
    public class AiFormConverter : IFormConverter
    {
        private readonly AppSettings _settings;

        // Parses <file name="Foo.cs">...content...</file> from the AI response
        private static readonly Regex FileBlockRegex = new(
            @"<file\s+name=""([^""]+)""\s*>([\s\S]*?)</file>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AiFormConverter(AppSettings settings)
        {
            _settings = settings;
        }

        // ── IFormConverter ────────────────────────────────────────────────────

        public async Task<ConversionResult> ConvertAsync(
            FormRecord          source,
            FormType            targetType,
            string              outputDirectory,
            string              outputBaseName,
            string              outputNamespace,
            IEnumerable<string> namespacesToComment,
            IProgress<string>?  progress,
            CancellationToken   cancellationToken)
        {
            var commentList = namespacesToComment.ToList();

            try
            {
                // ── 1. Read source files ──────────────────────────────────────
                progress?.Report("Reading source files…");
                var sourceFiles = ReadSourceFiles(source);

                // ── 2. Build prompts ──────────────────────────────────────────
                progress?.Report("Building conversion prompt…");
                var systemPrompt = ConversionPromptBuilder.BuildSystemPrompt();
                var userPrompt   = ConversionPromptBuilder.BuildUserPrompt(
                    source, targetType, sourceFiles,
                    outputBaseName, outputNamespace, commentList);

                // ── 3. Call AI ────────────────────────────────────────────────
                progress?.Report(
                    "Calling AI provider — this may take 30–90 seconds for large forms…");

                var provider = AiProviderRouter.GetProvider(
                    AiTask.CodeConversion, _settings);

                var rawResponse = await provider.GenerateAsync(
                    systemPrompt, userPrompt, cancellationToken);

                // ── 4. Parse output files ─────────────────────────────────────
                progress?.Report("Parsing AI response…");
                var outputFiles = ParseOutputFiles(rawResponse);

                if (outputFiles.Count == 0)
                    return ConversionResult.Fail(
                        "The AI did not return any recognisable output files.\r\n\r\n" +
                        "The response may have been truncated (form too large) or the AI " +
                        "did not follow the output format. Try a smaller form or check " +
                        "the AI provider settings.\r\n\r\n" +
                        "Raw response preview:\r\n" +
                        rawResponse[..Math.Min(500, rawResponse.Length)]);

                // ── 5. Write output files ─────────────────────────────────────
                progress?.Report($"Writing {outputFiles.Count} file(s)…");
                Directory.CreateDirectory(outputDirectory);

                var written = new List<string>();
                foreach (var (filename, content) in outputFiles)
                {
                    var fullPath = Path.Combine(outputDirectory, filename);
                    File.WriteAllText(fullPath, content.Trim());
                    written.Add(fullPath);
                }

                return new ConversionResult
                {
                    Success                  = true,
                    OutputFiles              = outputFiles,
                    WrittenPaths             = written,
                    CommentedOutNamespaces   = commentList,
                    RawAiResponse            = rawResponse
                };
            }
            catch (OperationCanceledException)
            {
                return ConversionResult.Fail("Conversion was cancelled.");
            }
            catch (InvalidOperationException ex)
            {
                // Typically: no API key configured
                return ConversionResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return ConversionResult.Fail(
                    $"Conversion failed: {ex.GetType().Name} — {ex.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads all source files that make up the given form record.
        /// Returns a dictionary of filename → file content.
        /// </summary>
        private static Dictionary<string, string> ReadSourceFiles(FormRecord record)
        {
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string mainPath = record.FormFilePath;
            if (!File.Exists(mainPath))
                throw new FileNotFoundException("Source form file not found.", mainPath);

            files[Path.GetFileName(mainPath)] = File.ReadAllText(mainPath);

            switch (record.FormType)
            {
                case FormType.WinFormsDesigner:
                    // Include companion .Designer.cs
                    var designerPath = Path.ChangeExtension(mainPath, null) + ".Designer.cs";
                    if (File.Exists(designerPath))
                        files[Path.GetFileName(designerPath)] = File.ReadAllText(designerPath);
                    break;

                case FormType.Xaml:
                    // Include companion .xaml.cs code-behind
                    var csPath = mainPath + ".cs";
                    if (File.Exists(csPath))
                        files[Path.GetFileName(csPath)] = File.ReadAllText(csPath);
                    break;
                    // WinFormsCodeOnly: single file already added above
            }

            return files;
        }

        /// <summary>
        /// Extracts all <c>&lt;file name="…"&gt;content&lt;/file&gt;</c> blocks
        /// from the AI response. Returns filename → content map.
        ///
        /// Falls back to using the entire response as a single file when the AI
        /// produced valid-looking code but forgot the wrapper tags.
        /// </summary>
        private static Dictionary<string, string> ParseOutputFiles(string response)
        {
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in FileBlockRegex.Matches(response))
            {
                var name    = m.Groups[1].Value.Trim();
                var content = m.Groups[2].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(content))
                    files[name] = content;
            }

            return files;
        }
    }
}
