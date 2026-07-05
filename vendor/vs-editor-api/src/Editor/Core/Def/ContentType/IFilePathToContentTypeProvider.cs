//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// MEF export to map full file names to a content type.
    /// </summary>
    /// <remarks>
    /// <para>Instances of this class should define the following MEF attributes.
    /// <code>
    ///    [Export(typeof(IFilePathToContentTypeProvider)]      -- Required
    ///    [Name("BamBam")]                                     -- Required
    ///    [Order(After = "Fred", Before="Barney")]             -- Optional, can have more than one.
    ///    [FileExtension(".abc")]                              -- Optional, but must have either a FileExtension or a FileName attribute
    ///    [FileName("George")]                                 -- Optional, but must have either a FileExtension or a FileName attribute
    /// </code>
    /// You can use "*" as the FileExtension attribute to match any file extension.</para>
    ///
    /// <para>
    /// The <see cref="IFilePathToContentTypeProvider"/> will be called in order (based on the <see cref="OrderAttribute"/>) if their
    /// <see cref="FileExtensionAttribute"/> matches the extension of the file in question (or is a "*") or the <see cref="FileNameAttribute"/>
    /// matches the name of the file in question.
    /// </para>
    /// </remarks>
    public interface IFilePathToContentTypeProvider
    {
        bool TryGetContentTypeForFilePath(string filePath, out IContentType contentType);
    }
}
