using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    /// <summary>
    /// Attribute defining whether or not the description has an associated option.
    /// </summary>
    /// <remarks>
    /// Defaults to true. Set to false for static elements in the options page.
    /// </remarks>
    public sealed class IsOptionAttribute : SingletonBaseMetadataAttribute
    {
        public IsOptionAttribute(bool isOption)
        {
            this.IsOption = isOption;
        }

        public bool IsOption { get; }
    }
}
