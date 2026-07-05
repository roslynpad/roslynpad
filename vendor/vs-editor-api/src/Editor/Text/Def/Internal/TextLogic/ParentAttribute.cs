using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    /// <summary>
    /// Attribute indicating the parent option/page of the option.
    /// </summary>
    /// <remarks>
    /// Options can be nested or attached directly to the their corresponding page.
    /// </remarks>
    public sealed class ParentAttribute : SingletonBaseMetadataAttribute
    {
        public ParentAttribute(string parent)
        {
            this.Parent = parent;
        }

        public string Parent { get; }
    }
}
