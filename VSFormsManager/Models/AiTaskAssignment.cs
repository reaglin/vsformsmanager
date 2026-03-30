namespace VSFormsManager.Models
{
    /// <summary>
    /// Stores a provider assignment for every <see cref="AiTask"/>.
    /// Serialised as part of <see cref="AppSettings"/> in the JSON settings file.
    /// </summary>
    public class AiTaskAssignment
    {
        public AiProviderType FormAnalysis   { get; set; } = AiProviderType.Claude;
        public AiProviderType CodeConversion { get; set; } = AiProviderType.Claude;

        /// <summary>Returns the provider assigned to <paramref name="task"/>.</summary>
        public AiProviderType GetProvider(AiTask task) => task switch
        {
            AiTask.FormAnalysis   => FormAnalysis,
            AiTask.CodeConversion => CodeConversion,
            _                     => AiProviderType.None
        };

        /// <summary>Assigns <paramref name="provider"/> to <paramref name="task"/>.</summary>
        public void SetProvider(AiTask task, AiProviderType provider)
        {
            switch (task)
            {
                case AiTask.FormAnalysis:   FormAnalysis   = provider; break;
                case AiTask.CodeConversion: CodeConversion = provider; break;
            }
        }
    }
}
