//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Specifies a mapping between a content type and a file extension.
    /// </summary>
    /// <remarks> 
    /// Because you cannot subclass this type, you can use the [Export] attribute with no type.
    /// </remarks>
    /// <example>
    /// internal sealed class Components
    /// {
    ///    [Export]
    ///    [FileExtension(".abc")]           // Any file with the extention "abc" will get the "alphabet" content type.
    ///    [ContentType("alphabet")]
    ///    internal FileExtensionToContentTypeDefinition abcFileExtensionDefinition;
    ///    
    ///    [Export]
    ///    [FileName("readme")]           // Any file named "readme" will get the "alphabet" content type.
    ///    [ContentType("alphabet")]
    ///    internal FileExtensionToContentTypeDefinition readmeFileNameDefinition;
    ///    { other components }
    /// }
    /// </example>
    public sealed class FileExtensionToContentTypeDefinition
    {
    }
}
