namespace VSFormsManager.Models
{
    /// <summary>
    /// Represents a single UI control discovered on a form during parsing.
    /// </summary>
    public class ControlInfo
    {
        /// <summary>The field/variable name used in code (e.g. <c>btnSave</c>).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The short type name of the control (e.g. <c>Button</c>, <c>TextBox</c>).
        /// Namespace prefixes are stripped during parsing.
        /// </summary>
        public string ControlType { get; set; } = string.Empty;

        public override string ToString() => $"{Name} ({ControlType})";
    }
}
