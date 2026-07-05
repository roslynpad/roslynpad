using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    /// <summary>
    /// Describes metadata required of <see cref="FeatureDefinition"/> imports.
    /// </summary>
    public interface IFeatureDefinitionMetadata
    {
        /// <summary>
        /// Name of the feature
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optionally, a collection of names of parent features
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        IEnumerable<string> BaseDefinition { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IFeatureDefinitionMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class FeatureDefinitionMetadata : IFeatureDefinitionMetadata
    {
        public FeatureDefinitionMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.BaseDefinition = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(BaseDefinition));
        }

        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> BaseDefinition { get; }
    }
}
