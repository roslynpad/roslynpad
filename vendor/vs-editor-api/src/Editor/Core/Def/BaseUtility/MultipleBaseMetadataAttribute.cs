//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Composition;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A base class for attributes that can appear multiple times on a single component part.
    /// </summary>
    [MetadataAttribute]
    [System.ComponentModel.Composition.MetadataAttribute] // for MEF v1 parts composed via VS-MEF
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
    public abstract class MultipleBaseMetadataAttribute : Attribute
    {
    }
}
