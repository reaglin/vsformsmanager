using VSFormsManager.Models;
using VSFormsManager.Services;

namespace VSFormsManager
{
    /// <summary>
    /// Holds application-wide state that needs to be accessible across forms
    /// without passing it through constructors everywhere.
    ///
    /// Initialise once at startup in Program.cs:
    ///   AppSession.Initialise();
    ///
    /// Open the settings form from any form via:
    ///   AppSession.OpenAiSettings(this);
    /// </summary>
    public static class AppSession
    {
        // ── Settings ──────────────────────────────────────────────────────────

        /// <summary>Current application settings. Populated on first call to Initialise().</summary>
        public static AppSettings Settings { get; private set; } = new();

        /// <summary>
        /// Loads settings from disk. Call once from Program.cs before Application.Run().
        /// Safe to call multiple times — each call reloads from disk.
        /// </summary>
        public static void Initialise()
        {
            Settings = SettingsManager.Load();
        }

        /// <summary>
        /// Reloads settings from disk. Use after the settings form saves changes
        /// so the rest of the app immediately sees the new values.
        /// </summary>
        public static void ReloadSettings()
        {
            Settings = SettingsManager.Load();
        }

        // ── Current form ──────────────────────────────────────────────────────

        /// <summary>
        /// The form record currently selected for display and conversion.
        /// Set by <see cref="frmGetForm"/> when the user clicks "Use This Form".
        /// Read by <see cref="frmMain"/> to populate the detail panel and
        /// enable the "Save Form As" button.
        /// </summary>
        public static FormRecord? CurrentForm { get; set; }

        // ── Dialog helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Opens <see cref="frmAiSettings"/> as a modal dialog owned by
        /// <paramref name="owner"/>. Reloads settings if the user saves.
        /// </summary>
        /// <returns>True if the user saved changes.</returns>
        public static bool OpenAiSettings(IWin32Window owner)
        {
            using var dlg = new frmAiSettings(Settings);
            if (dlg.ShowDialog(owner) == DialogResult.OK)
            {
                ReloadSettings();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens <see cref="frmGetForm"/> as a modal dialog. If the user clicks
        /// "Use This Form", <see cref="CurrentForm"/> is updated before the dialog
        /// closes and this method returns <c>true</c>.
        /// </summary>
        public static bool OpenFormBrowser(IWin32Window owner)
        {
            using var dlg = new frmGetForm();
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }
    }
}
