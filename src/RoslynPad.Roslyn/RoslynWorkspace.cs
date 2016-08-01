// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn
{
    public sealed class RoslynWorkspace : Workspace
    {
        private readonly NuGetConfiguration _nuGetConfiguration;
        private readonly ConcurrentDictionary<string, DirectiveInfo> _referencesDirectives;

        public RoslynHost RoslynHost { get; }
        public DocumentId OpenDocumentId { get; private set; }

        internal RoslynWorkspace(HostServices host, NuGetConfiguration nuGetConfiguration, RoslynHost roslynHost)
            : base(host, WorkspaceKind.Host)
        {
            _nuGetConfiguration = nuGetConfiguration;
            _referencesDirectives = new ConcurrentDictionary<string, DirectiveInfo>();

            RoslynHost = roslynHost;
        }

        public new void SetCurrentSolution(Solution solution)
        {
            var oldSolution = CurrentSolution;
            var newSolution = base.SetCurrentSolution(solution);
            RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
        }

        public override bool CanOpenDocuments => true;

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                    return true;
                default:
                    return false;
            }
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            OpenDocumentId = documentId;
            OnDocumentOpened(documentId, textContainer);
            OnDocumentContextUpdated(documentId);
        }

        public event Action<DocumentId, SourceText> ApplyingTextChange;

        protected override void Dispose(bool finalize)
        {
            base.Dispose(finalize);

            ApplyingTextChange = null;
        }

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            if (OpenDocumentId != document)
            {
                return;
            }

            ApplyingTextChange?.Invoke(document, newText);

            OnDocumentTextChanged(document, newText, PreservationMode.PreserveIdentity);
        }

        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        internal void ClearOpenDocument(DocumentId documentId)
        {
            base.ClearOpenDocument(documentId);
        }

        internal new void RegisterText(SourceTextContainer textContainer)
        {
            base.RegisterText(textContainer);
        }

        internal new void UnregisterText(SourceTextContainer textContainer)
        {
            base.UnregisterText(textContainer);
        }


        private class DirectiveInfo
        {
            public MetadataReference MetadataReference { get; }

            public bool IsActive { get; set; }

            public DirectiveInfo(MetadataReference metadataReference)
            {
                MetadataReference = metadataReference;
                IsActive = true;
            }
        }

        internal async Task ProcessReferenceDirectives(Document document)
        {
            var project = document.Project;
            var directives = ((CompilationUnitSyntax)await document.GetSyntaxRootAsync().ConfigureAwait(false))
                .GetReferenceDirectives().Select(x => x.File.ValueText).ToImmutableHashSet();

            var changed = false;
            foreach (var referenceDirective in _referencesDirectives)
            {
                if (referenceDirective.Value.IsActive && !directives.Contains(referenceDirective.Key))
                {
                    referenceDirective.Value.IsActive = false;
                    changed = true;
                }
            }

            foreach (var directive in directives)
            {
                DirectiveInfo referenceDirective;
                if (_referencesDirectives.TryGetValue(directive, out referenceDirective))
                {
                    if (!referenceDirective.IsActive)
                    {
                        referenceDirective.IsActive = true;
                        changed = true;
                    }
                }
                else
                {
                    if (_referencesDirectives.TryAdd(directive, new DirectiveInfo(ResolveReference(directive))))
                    {
                        changed = true;
                    }
                }
            }
            
            if (!changed) return;

            lock (_referencesDirectives)
            {
                var solution = project.Solution;
                var references =
                    _referencesDirectives.Where(x => x.Value.IsActive)
                        .Select(x => x.Value.MetadataReference)
                        .WhereNotNull();
                var newSolution = solution.WithProjectMetadataReferences(project.Id,
                    RoslynHost.DefaultReferences.Concat(references));

                SetCurrentSolution(newSolution);
            }
        }

        private MetadataReference ResolveReference(string name)
        {
            if (_nuGetConfiguration != null)
            {
                name = _nuGetConfiguration.ResolveReference(name);
            }
            if (File.Exists(name))
            {
                return RoslynHost.CreateMetadataReference(name);
            }
            try
            {
                var assemblyName = GlobalAssemblyCache.Instance.ResolvePartialName(name);
                if (assemblyName == null)
                {
                    return null;
                }
                var assembly = Assembly.Load(assemblyName.ToString());
                return RoslynHost.CreateMetadataReference(assembly.Location);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool HasReference(string text)
        {
            DirectiveInfo info;
            if (_referencesDirectives.TryGetValue(text, out info))
            {
                return info.IsActive;
            }
            return false;
        }
    }
}
