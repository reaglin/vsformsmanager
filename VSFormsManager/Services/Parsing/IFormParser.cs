using VSFormsManager.Models;

namespace VSFormsManager.Services.Parsing
{
    /// <summary>
    /// Contract for a parser that reads a Visual Studio form from disk and
    /// returns a populated <see cref="FormRecord"/>.
    ///
    /// Each implementation handles one <see cref="FormType"/> (WinForms Designer,
    /// WinForms Code-Only, XAML). <see cref="FormParserFactory"/> selects the
    /// correct implementation based on the file at hand.
    ///
    /// Implementations should be stateless — a single instance can parse many files.
    /// </summary>
    public interface IFormParser
    {
        /// <summary>
        /// Parses the form at <paramref name="primaryFilePath"/> and returns a
        /// fully-populated <see cref="FormRecord"/>.
        ///
        /// <paramref name="primaryFilePath"/> is always the main source file:
        /// the <c>.cs</c> file for WinForms or the <c>.xaml</c> file for XAML.
        ///
        /// Throws <see cref="InvalidOperationException"/> if the file cannot be parsed
        /// as the expected form type. Throws <see cref="IOException"/> on read errors.
        /// </summary>
        FormRecord Parse(string primaryFilePath);
    }
}
