//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// Attempts to detect a text encoding associated with a stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(NameSource=typeof(IEncodingDetector))]
    /// </para>
    /// <para>
    /// Exports must include a [Name] attribute and at least one [ContentType] attribute.
    /// Exports may optionally include the [Order] attribute.
    /// </para>
    /// </remarks>
    public interface IEncodingDetector
    {
        /// <summary>
        /// Attempts to detect an encoding associated with a stream.
        /// </summary>
        /// <remarks>
        /// The stream is read from its current position. The encoding sniffer does not need to reset the stream's position.
        /// </remarks>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The detected encoding, or null if one could not be determined.</returns>
        Encoding GetStreamEncoding(Stream stream);
    }
}
