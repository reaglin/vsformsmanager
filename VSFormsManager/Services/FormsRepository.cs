using System.Text.Json;
using System.Text.Json.Serialization;
using VSFormsManager.Models;

namespace VSFormsManager.Services
{
    /// <summary>
    /// Manages the persistent list of discovered forms stored in
    /// <c>%APPDATA%\VSFormsManager\forms.json</c>.
    ///
    /// Records are keyed by <see cref="FormRecord.FormFilePath"/> (case-insensitive)
    /// so rescanning the same file updates the existing record rather than
    /// creating a duplicate.
    ///
    /// Usage:
    ///   var repo = new FormsRepository();   // loads from disk
    ///   repo.AddOrUpdate(record);           // saves automatically
    ///   var all = repo.GetAll();
    /// </summary>
    public class FormsRepository
    {
        // ── Paths ─────────────────────────────────────────────────────────────

        private static readonly string FormsFilePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VSFormsManager",
                "forms.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented      = true,
            Converters         = { new JsonStringEnumConverter() }
        };

        // ── In-memory list ────────────────────────────────────────────────────

        private List<FormRecord> _records;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>Creates a repository and loads any existing records from disk.</summary>
        public FormsRepository()
        {
            _records = LoadFromDisk();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>All currently stored form records (read-only view).</summary>
        public IReadOnlyList<FormRecord> GetAll() => _records.AsReadOnly();

        /// <summary>
        /// Returns the record whose <see cref="FormRecord.FormFilePath"/> matches
        /// <paramref name="filePath"/> (case-insensitive), or <c>null</c> if not found.
        /// </summary>
        public FormRecord? GetByFilePath(string filePath) =>
            _records.FirstOrDefault(r =>
                r.FormFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Adds a new record or replaces an existing record with the same
        /// <see cref="FormRecord.FormFilePath"/>. Persists to disk immediately.
        /// </summary>
        public void AddOrUpdate(FormRecord record)
        {
            var index = _records.FindIndex(r =>
                r.FormFilePath.Equals(record.FormFilePath, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                // Preserve the original Id so tree nodes and external references remain stable
                record.Id = _records[index].Id;
                _records[index] = record;
            }
            else
            {
                _records.Add(record);
            }

            SaveToDisk();
        }

        /// <summary>
        /// Removes the record for the given <paramref name="record"/> from the list
        /// and persists the change to disk.
        /// </summary>
        public void Remove(FormRecord record)
        {
            _records.RemoveAll(r =>
                r.FormFilePath.Equals(record.FormFilePath, StringComparison.OrdinalIgnoreCase));

            SaveToDisk();
        }

        /// <summary>
        /// Returns records grouped by project name, sorted alphabetically.
        /// Within each project, records are sorted by <see cref="FormRecord.FormName"/>.
        /// </summary>
        public IEnumerable<IGrouping<string, FormRecord>> GetGroupedByProject() =>
            _records
                .OrderBy(r => r.ProjectName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.FormName,    StringComparer.OrdinalIgnoreCase)
                .GroupBy(r => r.ProjectName);

        // ── Persistence ───────────────────────────────────────────────────────

        private static List<FormRecord> LoadFromDisk()
        {
            try
            {
                if (!File.Exists(FormsFilePath))
                    return new List<FormRecord>();

                var json = File.ReadAllText(FormsFilePath);
                return JsonSerializer.Deserialize<List<FormRecord>>(json, JsonOptions)
                       ?? new List<FormRecord>();
            }
            catch
            {
                return new List<FormRecord>();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var dir = Path.GetDirectoryName(FormsFilePath)!;
                Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_records, JsonOptions);
                File.WriteAllText(FormsFilePath, json);
            }
            catch (Exception ex)
            {
                // Re-throw so callers can surface the error to the user
                throw new IOException($"Could not save forms list: {ex.Message}", ex);
            }
        }
    }
}
