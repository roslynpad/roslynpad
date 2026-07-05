using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Defines a feature which may be disabled using <see cref="IFeatureService"/> and grouped using <see cref="BaseDefinitionAttribute"/>
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you can use the [Export] attribute with no type.
    /// </remarks>
    /// <example>
    /// [Export]
    /// [Name(nameof(MyFeature))]   // required
    /// [BaseDefinition(PredefinedEditorFeatureNames.Popup)]   // zero or more BaseDefinitions are allowed
    /// public FeatureDefinition MyFeature;
    /// </example>
    public sealed class FeatureDefinition
    {
    }
}
