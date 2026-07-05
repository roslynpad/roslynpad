using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    /// <summary>
    /// Attribute defining whether or not the option roams.
    /// </summary>
    /// <remarks>
    /// If not provided, then the option is not considered a roaming attribute.
    /// </remarks>
    public sealed class IsRoamingAttribute : SingletonBaseMetadataAttribute
    {
        public IsRoamingAttribute(bool isRoaming = true)
        {
            this.IsRoaming = isRoaming;
        }

        public bool IsRoaming { get; }
    }
}
