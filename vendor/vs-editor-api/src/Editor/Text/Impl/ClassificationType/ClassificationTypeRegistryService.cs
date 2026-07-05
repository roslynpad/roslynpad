//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Text;
    using Microsoft.VisualStudio.Utilities;

    public interface IClassificationTypeDefinitionMetadata
    {
        string Name { get; }
        [System.ComponentModel.DefaultValue(null)]
        IEnumerable<string> BaseDefinition { get; }
    }

    [Export(typeof(IClassificationTypeRegistryService))]
    [Shared]
    public sealed class ClassificationTypeRegistryService : IClassificationTypeRegistryService
    {
        [ImportMany]
        public Lazy<ClassificationTypeDefinition, ClassificationTypeDefinitionMetadata>[] _classificationTypeDefinitions { get; set; }

        [Export]
        [Name("(TRANSIENT)")]
        public ClassificationTypeDefinition transientClassificationType { get; set; }

        [Export]
        [Name("text")]
        public ClassificationTypeDefinition textClassificationType { get; set; }

        #region Private Members
        private Dictionary<ClassificationKey, ClassificationTypeImpl> _classificationTypes;
        private Dictionary<string, ClassificationTypeImpl> _transientClassificationTypes;

        #endregion // Private Members

        #region Public Members
        public IClassificationType GetClassificationType(string type)
        {
            return this.GetClassificationType(ClassificationLayer.Default, type);
        }

        public ILayeredClassificationType GetClassificationType(ClassificationLayer layer, string type)
        {
            if (!this.ClassificationTypes.TryGetValue(new ClassificationKey(type, layer), out ClassificationTypeImpl classificationType)
                && layer != ClassificationLayer.Default)
            {
                // MEF-contributed definitions are registered in the default layer; requests for
                // other layers fall back to it (Roslyn resolves its classification types through
                // the semantic layer and expects to find the MEF-registered ones).
                this.ClassificationTypes.TryGetValue(new ClassificationKey(type, ClassificationLayer.Default), out classificationType);
            }

            return classificationType;
        }

        /// <summary>
        /// Create a new classification type and add it to the registry.
        /// </summary>
        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
        {
            return this.CreateClassificationType(ClassificationLayer.Default, type, baseTypes);
        }

        public ILayeredClassificationType CreateClassificationType(ClassificationLayer layer, string type, IEnumerable<IClassificationType> baseTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (baseTypes == null)
            {
                throw new ArgumentNullException(nameof(baseTypes));
            }

            ClassificationKey key = new ClassificationKey(type, layer);
            if (ClassificationTypes.ContainsKey(key))
            {
                throw new InvalidOperationException(LookUp.Strings.ClassificationAlreadyAdded);
            }

            // Use the non-canonical name for the actual type
            ClassificationTypeImpl classificationType = new ClassificationTypeImpl(type);
            foreach (var baseType in baseTypes)
            {
                classificationType.AddBaseType(baseType);
            }

            ClassificationTypes.Add(key, classificationType);

            return classificationType;
        }

        /// <summary>
        /// Create a transient classification type that can be used to represent
        /// classification types generated at runtime.
        /// </summary>
        /// <param name="baseTypes">The base types associated with this transient type.</param>
        /// <returns>The new transient type.</returns>
        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        {
            // Validate
            if (baseTypes == null)
            {
                throw new ArgumentNullException(nameof(baseTypes));
            }
            if (!baseTypes.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException(LookUp.Strings.TransientTypesNeedAtLeastOneBaseType);
            }

            return BuildTransientClassificationType(baseTypes);
        }

        /// <summary>
        /// Create a transient classification type that can be used to represent
        /// classification types generated at runtime.
        /// </summary>
        /// <param name="baseTypes">The base types associated with this transient type.</param>
        /// <returns>The new transient type.</returns>
        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
        {
            // Validate
            if (baseTypes == null)
            {
                throw new ArgumentNullException(nameof(baseTypes));
            }
            if (baseTypes.Length == 0)
            {
                throw new InvalidOperationException(LookUp.Strings.TransientTypesNeedAtLeastOneBaseType);
            }

            return BuildTransientClassificationType(baseTypes);
        }
        #endregion // Public Members

        #region Private Methods

        /// <summary>
        /// The transient type contributed by this assembly.
        /// </summary>
        private IClassificationType TransientClassificationType
        {
            get
            {
                return ClassificationTypes[new ClassificationKey("(TRANSIENT)")];
            }
        }

        /// <summary>
        /// The map of classification type names to actual IClassificationTypes.
        /// 
        /// Used to lazily init the map.
        /// </summary>
        private Dictionary<ClassificationKey, ClassificationTypeImpl> ClassificationTypes
        {
            get
            {
                if (_classificationTypes == null)
                {
                    _classificationTypes = new Dictionary<ClassificationKey, ClassificationTypeImpl>();
                    BuildClassificationTypes(_classificationTypes);
                }
                return _classificationTypes;
            }
        }

        /// <summary>
        /// Consumes all of the IClassificationTypeProvisions in the system to build the 
        /// list of classification types in the system.
        /// </summary>
        private void BuildClassificationTypes(Dictionary<ClassificationKey, ClassificationTypeImpl> classificationTypes)
        {
            // For each content baseType provision, create an IClassificationType.
            foreach (Lazy<ClassificationTypeDefinition, ClassificationTypeDefinitionMetadata> classificationTypeDefinition in _classificationTypeDefinitions)
            {
                string classificationName = classificationTypeDefinition.Metadata.Name;
                ClassificationKey key = new ClassificationKey(classificationName);
                ClassificationTypeImpl type = null;

                if (!classificationTypes.TryGetValue(key, out type))
                {
                    type = new ClassificationTypeImpl(classificationName);
                    classificationTypes.Add(key, type);
                }

                IEnumerable<string> baseTypes = classificationTypeDefinition.Metadata.BaseDefinition;
                if (baseTypes != null)
                {
                    ClassificationTypeImpl baseType = null;

                    foreach (string baseClassificationType in baseTypes)
                    {
                        ClassificationKey baseKey = new ClassificationKey(baseClassificationType);
                        if (!classificationTypes.TryGetValue(baseKey, out baseType))
                        {
                            baseType = new ClassificationTypeImpl(baseClassificationType);
                            classificationTypes.Add(baseKey, baseType);
                        }

                        type.AddBaseType(baseType);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a new transient classification type based on a set of actual base
        /// types.
        /// 
        /// With multiple projection buffers, it is possible to have a transient classification
        /// type with transient types as parents.
        /// </summary>
        /// <param name="baseTypes"></param>
        /// <returns></returns>
        private IClassificationType BuildTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        {
            // Lazily init
            if (_transientClassificationTypes == null)
            {
                _transientClassificationTypes = new Dictionary<string, ClassificationTypeImpl>(StringComparer.OrdinalIgnoreCase);
            }

            List<IClassificationType> sortedBaseTypes = new List<IClassificationType>(baseTypes);
            sortedBaseTypes.Sort(delegate(IClassificationType a, IClassificationType b)
                                 { return string.CompareOrdinal(a.Classification, b.Classification); });

            // Build the transient name
            StringBuilder sb = new StringBuilder();
            foreach (IClassificationType type in sortedBaseTypes)
            {
                sb.Append(type.Classification);
                sb.Append(" - ");
            }

            // Append "(transient)" onto the name.
            sb.Append(this.TransientClassificationType.Classification);

            // Look for a cached type
            ClassificationTypeImpl transientType;
            if (!_transientClassificationTypes.TryGetValue(sb.ToString(), out transientType))
            {
                // Didn't find a cached type, so create a new one
                transientType = new ClassificationTypeImpl(sb.ToString());

                foreach (IClassificationType type in sortedBaseTypes)
                {
                    transientType.AddBaseType(type);
                }

                // Add in the transient type as a base type
                transientType.AddBaseType(TransientClassificationType);

                // Cache this type so it doesn't need to be created again.
                _transientClassificationTypes[transientType.Classification] = transientType;
            }

            return transientType;
        }

        #endregion // Private Methods
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IClassificationTypeDefinitionMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class ClassificationTypeDefinitionMetadata : IClassificationTypeDefinitionMetadata
    {
        public ClassificationTypeDefinitionMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.BaseDefinition = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(BaseDefinition));
        }

        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> BaseDefinition { get; }
    }
}
