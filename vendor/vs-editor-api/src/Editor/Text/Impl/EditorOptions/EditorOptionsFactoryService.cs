//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorOptions.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IEditorOptionsFactoryService))]
    [Export(typeof(IEditorOptionsFactoryService2))]
    [Shared]
    public sealed class EditorOptionsFactoryService : IEditorOptionsFactoryService2
    {
        [ImportMany]
        public Lazy<EditorOptionDefinition, NameMetadata>[] OptionImports { get; set; }

        private EditorOptions _globalOptions;
        private IDictionary<string, EditorOptionDefinition> _instantiatedOptionDefinitions = new Dictionary<string, EditorOptionDefinition>();
        private IDictionary<string, Lazy<EditorOptionDefinition, NameMetadata>> _namedOptionImports = new Dictionary<string, Lazy<EditorOptionDefinition, NameMetadata>>();

        [Import]
        public GuardedOperations guardedOperations { get; set; } = null;

        #region IEditorOptionsFactoryService Members
        public IEditorOptions GetOptions(IPropertyOwner scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            return scope.Properties.GetOrCreateSingletonProperty<IEditorOptions>(() => new EditorOptions(this.GlobalOptions as EditorOptions, scope, this));
        }

        public IEditorOptions CreateOptions()
        {
            return this.CreateOptions(allowsLateBinding: false);
        }

        public IEditorOptions GlobalOptions
        {
            get
            {
                if (_globalOptions == null)
                {
                    //We're guaranteed that the first thing that happens when anyone tries to create options is that the global options will be created first,
                    //so do initialization here.
                    _globalOptions = new EditorOptions(null, null, this);

                    //Initialize _after_ setting _globalOptions so that -- since this is a property -- we will only be initialized once if stepping through
                    //this code in the debugger (and trying to evaluate the .GlobalOptions in the watch window.
                    this.Initialize();
                }

                return _globalOptions;
            }
        }
        #endregion

        #region IEditorOptionsFactoryService2 Members
        public bool TryBindToScope(IEditorOptions options, IPropertyOwner scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            var editorOptions = options as EditorOptions;
            if ((editorOptions == null) || (!editorOptions.AllowsLateBinding) || scope.Properties.ContainsProperty(typeof(IEditorOptions)))
            {
                // options cannot be bound to the specified scope.
                return false;
            }

            editorOptions.SetScope(scope);
            scope.Properties.AddProperty(typeof(IEditorOptions), options);
            return true;
        }

        public IEditorOptions CreateOptions(bool allowsLateBinding)
        {
            return new EditorOptions(this.GlobalOptions as EditorOptions, null, this, allowsLateBinding: allowsLateBinding);
        }
        #endregion

        private void Initialize()
        {
            //Don't need to start locking things (yet)
            foreach (var import in this.OptionImports)
            {
                if (import.Metadata.Name != null)
                {
                    //The external user kindly provided a name as metadata.
                    this.SafeAdd(_namedOptionImports, import.Metadata.Name, import);

#if DEBUG
                    if (import.Metadata.Name.Contains('\\'))
                        System.Diagnostics.Debug.WriteLine("option with \\ " + import.Metadata.Name);
#endif
                }
                else
                {
                    //They didn't so we need to instantiate the extension in order to discover the name.
                    var definition = this.guardedOperations.InstantiateExtension(import, import);

                    this.SafeAdd(_instantiatedOptionDefinitions, definition.Name, definition);


#if DEBUG
                    System.Diagnostics.Debug.WriteLine("unnamed option: " + definition.Name);
#endif
                }
            }
        }

        private void SafeAdd<T>(IDictionary<string, T> dictionary, string name, T value)
        {
            try
            {
                dictionary.Add(name, value);
            }
            catch (ArgumentException)
            {
                this.guardedOperations.HandleException(this,
                                                       new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Duplicate EditorOptionDefinition named {0}", name)));
            }
        }

        public EditorOptionDefinition GetOptionDefinition(string optionId)
        {
            lock (_instantiatedOptionDefinitions)
            {
                EditorOptionDefinition definition;
                if (!_instantiatedOptionDefinitions.TryGetValue(optionId, out definition))
                {
                    Lazy<EditorOptionDefinition, NameMetadata> import;
                    if (_namedOptionImports.TryGetValue(optionId, out import))
                    {
                        definition = this.guardedOperations.InstantiateExtension(import, import);

                        _namedOptionImports.Remove(optionId);
                        _instantiatedOptionDefinitions.Add(optionId, definition);
                    }
                }

                return definition;
            }
        }

        internal EditorOptionDefinition GetOptionDefinitionOrThrow(string optionId)
        {
            var definition = this.GetOptionDefinition(optionId);
            if (definition == null)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "No EditorOptionDefinition export found for the given option name: {0}", optionId), nameof(optionId));

            return definition;
        }

        internal IEnumerable<EditorOptionDefinition> GetSupportedOptions(IPropertyOwner scope)
        {
            //Unfortunately, to make this work, we need to instantiate everything. Do it immediately so that
            //if someone does something like nesting calls to SupportedOptions we will have something stable.
            lock (_instantiatedOptionDefinitions)
            {
                foreach (var import in _namedOptionImports)
                {
                    var definition = this.guardedOperations.InstantiateExtension(import.Value, import.Value);
                    this.SafeAdd(_instantiatedOptionDefinitions, import.Key, definition);                       //Use the name from the metadata, not the name from the definition.
                }

                _namedOptionImports.Clear();
            }

            //At this point, _instantiatedOptionDefinitions should never change so we don't need to lock/copy.
            foreach (var definition in _instantiatedOptionDefinitions.Values)
            {
                if ((scope == null) || definition.IsApplicableToScope(scope))
                    yield return definition;
            }
        }

        internal IEnumerable<EditorOptionDefinition> GetInstantiatedOptions(IPropertyOwner scope)
        {
            List<EditorOptionDefinition> definitions;
            lock(_instantiatedOptionDefinitions)
            {
                definitions = _instantiatedOptionDefinitions.Values.ToList();
            }

            foreach (var definition in definitions)
            {
                if ((scope == null) || definition.IsApplicableToScope(scope))
                    yield return definition;
            }
        }
    }

    public interface INameMetadata
    {
        [DefaultValue(null)]
        string Name { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="INameMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class NameMetadata : INameMetadata
    {
        public NameMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
        }

        public string Name { get; }
    }
}
