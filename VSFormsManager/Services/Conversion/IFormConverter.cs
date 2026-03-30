using VSFormsManager.Models;

namespace VSFormsManager.Services.Conversion
{
    /// <summary>
    /// Contract for a service that converts a Visual Studio form from one
    /// <see cref="FormType"/> to another.
    ///
    /// The single implementation, <see cref="AiFormConverter"/>, delegates all
    /// conversion work to the configured AI provider.
    /// </summary>
    public interface IFormConverter
    {
        /// <summary>
        /// Converts <paramref name="source"/> to <paramref name="targetType"/>,
        /// writes the output files to <paramref name="outputDirectory"/>, and
        /// returns a <see cref="ConversionResult"/> describing the outcome.
        ///
        /// <paramref name="outputBaseName"/>  — base file name without extension
        ///                                       (e.g. <c>frmCustomer</c>).
        /// <paramref name="outputNamespace"/> — C# namespace to use in output files.
        /// <paramref name="namespacesToComment"/> — using-directive namespaces that
        ///     should be commented out because they are unavailable in the target project.
        /// <paramref name="progress"/>        — optional progress-message callback.
        /// </summary>
        Task<ConversionResult> ConvertAsync(
            FormRecord             source,
            FormType               targetType,
            string                 outputDirectory,
            string                 outputBaseName,
            string                 outputNamespace,
            IEnumerable<string>    namespacesToComment,
            IProgress<string>?     progress,
            CancellationToken      cancellationToken);
    }
}
