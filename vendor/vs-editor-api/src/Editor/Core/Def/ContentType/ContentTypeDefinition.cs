//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Defines a content type.
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you can use the [Export] attribute with no type.
    /// </remarks>
    /// <example>
    /// internal sealed class Components
    /// {
    ///    [Export]
    ///    [Name("Example")]            // required
    ///    [BaseDefinition("text")]     // zero or more BaseDefinitions are allowed
    ///    internal ContentTypeDefinition exampleDefinition;
    ///    
    ///    { other components }
    /// }
    /// </example>
    public sealed class ContentTypeDefinition
    {
    }
}

