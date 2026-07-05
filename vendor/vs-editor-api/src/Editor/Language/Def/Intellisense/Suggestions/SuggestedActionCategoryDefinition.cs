// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a suggested action category.
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you should use the [Export] attribute with no type.
    /// </remarks>
    /// <code>
    /// internal sealed class Components
    /// {
    ///     [Export]
    ///     [Name(PredefinedSuggestedActionCategoryNames.ErrorFix)]             // required
    ///     [BaseDefinition(PredefinedSuggestedActionCategoryNames.CodeFix)]    // zero or more BaseDefinitions are allowed
    ///     [Order]                                                             // indicates precedence of this category
    ///     private SuggestedActionCategoryDefinition errorFixSuggestedActionCategoryDefinition;
    ///    
    ///    { other components }
    /// }
    /// </code>
    public sealed class SuggestedActionCategoryDefinition
    {
    }
}
