//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    [DebuggerDisplay("{key}")]
    internal class ClassificationTypeImpl : ILayeredClassificationType, IClassificationType
    {
        private ClassificationKey key;
        private FrugalList<IClassificationType> baseTypes;

        internal ClassificationTypeImpl(string name) : this(new ClassificationKey(name))
        {
        }

        internal ClassificationTypeImpl(ClassificationKey key)
        {
            this.key = key;
        }

        internal void AddBaseType(IClassificationType baseType)
        {
            if (this.baseTypes == null)
            {
                this.baseTypes = new FrugalList<IClassificationType>();
            }

            this.baseTypes.Add(baseType);
        }

        public string Classification => this.key.Name;
        public ClassificationLayer Layer => this.key.Layer;

        public bool IsOfType(string type)
        {
            if (string.Equals(this.key.Name, type, System.StringComparison.OrdinalIgnoreCase))
                return true;
            else if (this.baseTypes != null)
            {
                foreach (IClassificationType baseType in this.baseTypes)
                {
                    if ( baseType.IsOfType(type) )
                        return true;
                }
            }

            return false;
        }

        public IEnumerable<IClassificationType> BaseTypes
        {
            get { return (this.baseTypes != null) ? (IEnumerable<IClassificationType>)(this.baseTypes.AsReadOnly()) : Enumerable.Empty<IClassificationType>(); }
        }

        public override string ToString()
        {
            return this.key.ToString();
        }
    }
}
