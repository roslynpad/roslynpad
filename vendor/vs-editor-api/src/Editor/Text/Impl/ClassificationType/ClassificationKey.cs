//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    [DebuggerDisplay("{Name,nq}, {Layer}")]
    internal struct ClassificationKey : IEquatable<ClassificationKey>
    {
        public ClassificationKey(string name, ClassificationLayer layer = ClassificationLayer.Default) : this()
        {
            this.Name = name ?? string.Empty;
            this.Layer = layer;
        }

        public string Name { get; }
        public ClassificationLayer Layer { get; }

        public bool Equals(ClassificationKey other)
        {
            return this.Layer == other.Layer && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj is ClassificationKey other)
                return this.Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name) ^ this.Layer.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Name}, layer={this.Layer}";
        }
    }
}
