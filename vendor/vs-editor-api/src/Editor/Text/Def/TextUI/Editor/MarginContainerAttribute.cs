//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the type of margin container.
    /// </summary>
    [MetadataAttribute]
    [System.ComponentModel.Composition.MetadataAttribute] // for MEF v1 parts composed via VS-MEF
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MarginContainerAttribute : SingletonBaseMetadataAttribute
    {
        private readonly string marginContainer;

        /// <summary>
        /// Instantiates a new instance of a <see cref="MarginContainerAttribute"/>.
        /// </summary>
        /// <param name="marginContainer">The name of the container for this margin.</param>
        /// <exception cref="ArgumentNullException"><paramref name="marginContainer"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="marginContainer"/> is an empty string.</exception>
        public MarginContainerAttribute(string marginContainer)
        {
            if (marginContainer == null)
                throw new ArgumentNullException(nameof(marginContainer));
            if (marginContainer.Length == 0)
                throw new ArgumentException("marginContainer is an empty string.");

            this.marginContainer = marginContainer;
        }

        /// <summary>
        /// The name of the margin container.
        /// </summary>
        public string MarginContainer
        {
            get 
            { 
                return this.marginContainer; 
            }
        }
    }
}