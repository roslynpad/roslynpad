// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Composition;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Indicates that an <see cref="IPeekableItemSourceProvider"/> supports standalone (not part of a project) files.
    /// The default value is false so the absense of this attribute on an <see cref="IPeekableItemSourceProvider"/> means
    /// it doesn't support standalone files.
    /// </summary>
    [MetadataAttribute]
    [System.ComponentModel.Composition.MetadataAttribute] // for MEF v1 parts composed via VS-MEF
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SupportsStandaloneFilesAttribute: Attribute
    {
        /// <summary>
        /// Gets whether an <see cref="IPeekableItemSourceProvider"/> supports standalone (not part of a project) files.
        /// </summary>
        public bool SupportsStandaloneFiles { get; private set; }

        /// <summary>
        /// Creates new insatnce of the <see cref="SupportsStandaloneFilesAttribute"/> class.
        /// </summary>
        /// <param name="supportsStandaloneFiles">Sets whether an <see cref="IPeekableItemSourceProvider"/> supports 
        /// standalone (not part of a project) files.</param>
        public SupportsStandaloneFilesAttribute(bool supportsStandaloneFiles)
        {
            this.SupportsStandaloneFiles = supportsStandaloneFiles;
        }
    }
}
