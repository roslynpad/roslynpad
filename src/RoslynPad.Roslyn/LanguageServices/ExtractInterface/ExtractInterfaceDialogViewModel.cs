// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Glyph = RoslynPad.Roslyn.Completion.Glyph;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface
{
    internal enum InterfaceDestination
    {
        CurrentFile,
        NewFile
    }

    internal class ExtractInterfaceDialogViewModel : NotificationObject
    {
        private readonly object _syntaxFactsService;
        private readonly List<string> _conflictingTypeNames;
        private readonly string _defaultNamespace;
        private readonly string _generatedNameTypeParameterSuffix;
        private readonly string _languageName;
        private readonly string _fileExtension;

        internal ExtractInterfaceDialogViewModel(
            object syntaxFactsService,
            string defaultInterfaceName,
            List<ISymbol> extractableMembers,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName,
            string fileExtension)
        {
            _syntaxFactsService = syntaxFactsService;
            _interfaceName = defaultInterfaceName;
            _conflictingTypeNames = conflictingTypeNames;
            _fileExtension = fileExtension;
            _fileName = $"{defaultInterfaceName}{fileExtension}";
            _defaultNamespace = defaultNamespace;
            _generatedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;
            _languageName = languageName;

            MemberContainers = extractableMembers.Select(m => new MemberSymbolViewModel(m)).OrderBy(s => s.MemberName).ToList();
        }

        internal bool TrySubmit()
        {
            var trimmedInterfaceName = InterfaceName.Trim();
            var trimmedFileName = FileName.Trim();

            if (!MemberContainers.Any(c => c.IsChecked))
            {
                SendFailureNotification("YouMustSelectAtLeastOneMember");
                return false;
            }

            if (_conflictingTypeNames.Contains(trimmedInterfaceName))
            {
                SendFailureNotification("InterfaceNameConflictsWithTypeName");
                return false;
            }

            //if (!_syntaxFactsService.IsValidIdentifier(trimmedInterfaceName))
            //{
            //    SendFailureNotification($"InterfaceNameIsNotAValidIdentifier {_languageName}");
            //    return false;
            //}

            if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                SendFailureNotification("IllegalCharactersInPath");
                return false;
            }

            if (!Path.GetExtension(trimmedFileName).Equals(_fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                SendFailureNotification($"FileNameMustHaveTheExtension {_fileExtension}");
                return false;
            }

            // TODO: Deal with filename already existing

            return true;
        }

        private void SendFailureNotification(string message)
        {
            //_notificationService.SendNotification(message, severity: NotificationSeverity.Information);
        }

        internal void DeselectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = false;
            }
        }

        internal void SelectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = true;
            }
        }

        public List<MemberSymbolViewModel> MemberContainers { get; set; }

        private string _interfaceName;
        public string InterfaceName
        {
            get => _interfaceName;

            set
            {
                if (SetProperty(ref _interfaceName, value))
                {
                    FileName = $"{value.Trim()}{_fileExtension}";
                    OnPropertyChanged(nameof(GeneratedName));
                }
            }
        }

        public string GeneratedName =>
            $"{(string.IsNullOrEmpty(_defaultNamespace) ? string.Empty : _defaultNamespace + ".")}{_interfaceName.Trim()}{_generatedNameTypeParameterSuffix}"
            ;

        private InterfaceDestination _destination = InterfaceDestination.NewFile;
        public InterfaceDestination Destination
        {
            get { return _destination; }
            set
            {
                if (SetProperty(ref _destination, value))
                {
                    OnPropertyChanged(nameof(FileNameEnabled));
                }
            }
        }

        public bool FileNameEnabled => Destination == InterfaceDestination.NewFile;

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        internal class MemberSymbolViewModel : NotificationObject
        {
            public ISymbol MemberSymbol { get; }

            private static readonly SymbolDisplayFormat _memberDisplayFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            public MemberSymbolViewModel(ISymbol symbol)
            {
                MemberSymbol = symbol;
                _isChecked = true;
            }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set => SetProperty(ref _isChecked, value);
            }

            public string MemberName => MemberSymbol.ToDisplayString(_memberDisplayFormat);

            public Glyph Glyph => MemberSymbol.GetGlyph();
        }
    }
}