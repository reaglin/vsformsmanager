namespace VSFormsManager.Models
{
    /// <summary>
    /// Identifies the on-disk format of a Visual Studio form that has been
    /// discovered by the application.
    /// </summary>
    public enum FormType
    {
        /// <summary>Format could not be determined.</summary>
        Unknown = 0,

        /// <summary>
        /// Classic Windows Forms with a designer-generated partial class.
        /// Primary file: <c>MyForm.cs</c>  ·  Designer file: <c>MyForm.Designer.cs</c>
        /// </summary>
        WinFormsDesigner = 1,

        /// <summary>
        /// Windows Forms form written entirely in code — no companion designer file.
        /// Single file: <c>MyForm.cs</c>
        /// </summary>
        WinFormsCodeOnly = 2,

        /// <summary>
        /// WPF or MAUI XAML form.
        /// Primary file: <c>MyWindow.xaml</c>  ·  Code-behind: <c>MyWindow.xaml.cs</c>
        /// </summary>
        Xaml = 3
    }
}
