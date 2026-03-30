using System.Text.Json;
using VSFormsManager.Models;

namespace VSFormsManager.Services
{
    /// <summary>
    /// Loads and saves <see cref="AppSettings"/> as a JSON file located in:
    ///   %APPDATA%\VSFormsManager\settings.json
    ///
    /// API keys are encrypted on disk using DPAPI via <see cref="EncryptionHelper"/>.
    /// The JSON file contains <see cref="StoredSettings"/> (encrypted blobs + plain fields).
    /// <see cref="AppSettings"/> — which the rest of the app uses — always holds
    /// plain-text, decrypted values.
    ///
    /// Load() returns a default AppSettings if the file is missing or unreadable.
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "VSFormsManager");

        public static readonly string SettingsFilePath =
            Path.Combine(SettingsFolder, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="StoredSettings"/> from disk, decrypts the API keys,
        /// and returns a ready-to-use <see cref="AppSettings"/>.
        ///
        /// Returns a default <see cref="AppSettings"/> (empty keys, default models)
        /// when the file does not exist, is not valid JSON, or cannot be decrypted.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new AppSettings();

                var json   = File.ReadAllText(SettingsFilePath);
                var stored = JsonSerializer.Deserialize<StoredSettings>(json, JsonOptions);
                return stored?.ToAppSettings() ?? new AppSettings();
            }
            catch
            {
                // Any I/O or JSON error → start with clean defaults.
                return new AppSettings();
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Encrypts the API keys in <paramref name="settings"/>, serialises to JSON,
        /// and writes to disk.
        ///
        /// Creates the settings folder if it does not exist.
        /// Throws on I/O failure — callers should catch and report to the user.
        /// </summary>
        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsFolder);

            var stored = StoredSettings.FromAppSettings(settings);
            var json   = JsonSerializer.Serialize(stored, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
