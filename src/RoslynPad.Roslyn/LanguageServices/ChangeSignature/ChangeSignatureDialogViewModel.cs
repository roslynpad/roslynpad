// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal class ChangeSignatureDialogViewModel : NotificationObject
    {
        private readonly ParameterConfiguration _originalParameterConfiguration;

        private readonly ParameterViewModel? _thisParameter;
        private readonly List<ParameterViewModel> _parametersWithoutDefaultValues;
        private readonly List<ParameterViewModel> _parametersWithDefaultValues;
        private readonly ParameterViewModel? _paramsParameter;
        private readonly HashSet<IParameterSymbol> _disabledParameters = new HashSet<IParameterSymbol>();
        private readonly ImmutableArray<SymbolDisplayPart> _declarationParts;

        internal ChangeSignatureDialogViewModel(ParameterConfiguration parameters, ISymbol symbol)
        {
            _originalParameterConfiguration = parameters;

            int startingSelectedIndex = 0;

            if (parameters.ThisParameter != null)
            {
                startingSelectedIndex++;

                _thisParameter = new ParameterViewModel(this, parameters.ThisParameter);
                _disabledParameters.Add(parameters.ThisParameter.Symbol);
            }

            if (parameters.ParamsParameter != null)
            {
                _paramsParameter = new ParameterViewModel(this, parameters.ParamsParameter);
            }

            _declarationParts = symbol.ToDisplayParts(_symbolDeclarationDisplayFormat);

            _parametersWithoutDefaultValues = parameters.ParametersWithoutDefaultValues.OfType<ExistingParameter>().Select(p => new ParameterViewModel(this, p)).ToList();
            _parametersWithDefaultValues = parameters.RemainingEditableParameters.OfType<ExistingParameter>().Select(p => new ParameterViewModel(this, p)).ToList();
            SelectedIndex = startingSelectedIndex;
        }

        public int GetStartingSelectionIndex()
        {
            return _thisParameter == null ? 0 : 1;
        }

        public bool PreviewChanges { get; set; }

        public bool CanRemove
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                // index = thisParameter == null ? index : index - 1;

                return !AllParameters[index].IsRemoved;
            }
        }

        public bool CanRestore
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;

                if (index == 0 && _thisParameter != null)
                {
                    return false;
                }

                // index = thisParameter == null ? index : index - 1;

                return AllParameters[index].IsRemoved;
            }
        }

        internal void Remove()
        {
            if (_selectedIndex == null) return;
            AllParameters[_selectedIndex.Value].IsRemoved = true;
            OnPropertyChanged(nameof(AllParameters));
            OnPropertyChanged(nameof(SignatureDisplay));
            OnPropertyChanged(nameof(IsOkButtonEnabled));
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(CanRestore));
        }

        internal void Restore()
        {
            if (_selectedIndex == null) return;
            AllParameters[_selectedIndex.Value].IsRemoved = false;
            OnPropertyChanged(nameof(AllParameters));
            OnPropertyChanged(nameof(SignatureDisplay));
            OnPropertyChanged(nameof(IsOkButtonEnabled));
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(CanRestore));
        }

        internal ParameterConfiguration GetParameterConfiguration()
        {
            return new ParameterConfiguration(
                _originalParameterConfiguration.ThisParameter,
                _parametersWithoutDefaultValues.Where(p => !p.IsRemoved).Select(p => (Parameter)new ExistingParameter(p.ParameterSymbol)).ToImmutableArray(),
                _parametersWithDefaultValues.Where(p => !p.IsRemoved).Select(p => (Parameter)new ExistingParameter(p.ParameterSymbol)).ToImmutableArray(),
                (_paramsParameter == null || _paramsParameter.IsRemoved) ? null : new ExistingParameter(_paramsParameter.ParameterSymbol),
                selectedIndex: -1);
        }

        private static readonly SymbolDisplayFormat _symbolDeclarationDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeExplicitInterface |
                SymbolDisplayMemberOptions.IncludeAccessibility |
                SymbolDisplayMemberOptions.IncludeModifiers);

        private static readonly SymbolDisplayFormat _parameterDisplayFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeName);

        public ImmutableArray<TaggedText> SignatureDisplay
        {
            get
            {
                // TODO: Should probably use original syntax & formatting exactly instead of regenerating here
                var displayParts = GetSignatureDisplayParts();

                return displayParts.ToTaggedText();
            }
        }

        internal string TEST_GetSignatureDisplayText()
        {
            return string.Concat(GetSignatureDisplayParts().Select(p => p.ToString()));
        }

        private List<SymbolDisplayPart> GetSignatureDisplayParts()
        {
            var displayParts = new List<SymbolDisplayPart>();

            displayParts.AddRange(_declarationParts);
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));

            bool first = true;
            foreach (var parameter in AllParameters.Where(p => !p.IsRemoved))
            {
                if (!first)
                {
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ","));
                    displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
                }

                first = false;
                displayParts.AddRange(parameter.ParameterSymbol.ToDisplayParts(_parameterDisplayFormat));
            }

            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
            return displayParts;
        }

        public List<ParameterViewModel> AllParameters
        {
            get
            {
                var list = new List<ParameterViewModel>();
                if (_thisParameter != null)
                {
                    list.Add(_thisParameter);
                }

                list.AddRange(_parametersWithoutDefaultValues);
                list.AddRange(_parametersWithDefaultValues);

                if (_paramsParameter != null)
                {
                    list.Add(_paramsParameter);
                }

                return list;
            }
        }

        public bool CanMoveUp
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                index = _thisParameter == null ? index : index - 1;
                if (index <= 0 || index == _parametersWithoutDefaultValues.Count || index >= _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count)
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanMoveDown
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                index = _thisParameter == null ? index : index - 1;
                if (index < 0 || index == _parametersWithoutDefaultValues.Count - 1 || index >= _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count - 1)
                {
                    return false;
                }

                return true;
            }
        }

        internal void MoveUp()
        {
            Debug.Assert(CanMoveUp);
            if (SelectedIndex == null) return;

            var index = SelectedIndex.Value;
            index = _thisParameter == null ? index : index - 1;
            Move(index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : _parametersWithDefaultValues, index < _parametersWithoutDefaultValues.Count ? index : index - _parametersWithoutDefaultValues.Count, -1);
        }

        internal void MoveDown()
        {
            Debug.Assert(CanMoveDown);
            if (SelectedIndex == null) return;

            var index = SelectedIndex.Value;
            index = _thisParameter == null ? index : index - 1;
            Move(index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : _parametersWithDefaultValues, index < _parametersWithoutDefaultValues.Count ? index : index - _parametersWithoutDefaultValues.Count, 1);
        }

        private void Move(List<ParameterViewModel> list, int index, int delta)
        {
            var param = list[index];
            list.RemoveAt(index);
            list.Insert(index + delta, param);

            SelectedIndex += delta;

            OnPropertyChanged(nameof(AllParameters));
            OnPropertyChanged(nameof(SignatureDisplay));
            OnPropertyChanged(nameof(IsOkButtonEnabled));
        }

        internal bool TrySubmit()
        {
            return IsOkButtonEnabled;
        }

        private bool IsDisabled(ParameterViewModel parameterViewModel)
        {
            return _disabledParameters.Contains(parameterViewModel.ParameterSymbol);
        }

        private IList<ParameterViewModel> GetSelectedGroup()
        {
            var index = SelectedIndex;
            index = _thisParameter == null ? index : index - 1;
            return index < _parametersWithoutDefaultValues.Count ? _parametersWithoutDefaultValues : index < _parametersWithoutDefaultValues.Count + _parametersWithDefaultValues.Count ? _parametersWithDefaultValues : new List<ParameterViewModel>();
        }

        public bool IsOkButtonEnabled
        {
            get
            {
                return AllParameters.Any(p => p.IsRemoved) ||
                    !_parametersWithoutDefaultValues.Select(p => p.Parameter).SequenceEqual(_originalParameterConfiguration.ParametersWithoutDefaultValues) ||
                    !_parametersWithDefaultValues.Select(p => p.Parameter).SequenceEqual(_originalParameterConfiguration.RemainingEditableParameters);
            }
        }

        private int? _selectedIndex;
        public int? SelectedIndex
        {
            get => _selectedIndex;

            set
            {
                var newSelectedIndex = value == -1 ? null : value;
                if (newSelectedIndex == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = newSelectedIndex;

                OnPropertyChanged(nameof(CanMoveUp));
                OnPropertyChanged(nameof(CanMoveDown));
                OnPropertyChanged(nameof(CanRemove));
                OnPropertyChanged(nameof(CanRestore));
            }
        }

        public class ParameterViewModel
        {
            private readonly ChangeSignatureDialogViewModel _changeSignatureDialogViewModel;

            public ExistingParameter Parameter { get; }
            public IParameterSymbol ParameterSymbol => Parameter.Symbol;

            public ParameterViewModel(ChangeSignatureDialogViewModel changeSignatureDialogViewModel, ExistingParameter parameter)
            {
                _changeSignatureDialogViewModel = changeSignatureDialogViewModel;
                Parameter = parameter;
            }

            public string Modifier
            {
                get
                {
                    // Todo: support VB
                    switch (ParameterSymbol.RefKind)
                    {
                        case RefKind.Out:
                            return "out";
                        case RefKind.Ref:
                            return "ref";
                    }

                    if (ParameterSymbol.IsParams)
                    {
                        return "params";
                    }

                    if (_changeSignatureDialogViewModel._thisParameter != null &&
                        SymbolEqualityComparer.Default.Equals(ParameterSymbol, _changeSignatureDialogViewModel._thisParameter.ParameterSymbol))
                    {
                        return "this";
                    }

                    return string.Empty;
                }
            }

            public string Type => ParameterSymbol.Type.ToDisplayString(_parameterDisplayFormat);

            public string ParameterName => ParameterSymbol.Name;

            public string Default
            {
                get
                {
                    if (!ParameterSymbol.HasExplicitDefaultValue)
                    {
                        return string.Empty;
                    }

                    return ParameterSymbol.ExplicitDefaultValue == null
                        ? "null"
                        : ParameterSymbol.ExplicitDefaultValue is string
                            ? "\"" + ParameterSymbol.ExplicitDefaultValue + "\""
                            : ParameterSymbol.ExplicitDefaultValue.ToString();
                }
            }

            public bool IsDisabled => _changeSignatureDialogViewModel.IsDisabled(this);

            public bool NeedsBottomBorder
            {
                get
                {
                    if (this == _changeSignatureDialogViewModel._thisParameter)
                    {
                        return true;
                    }

                    if (this == _changeSignatureDialogViewModel._parametersWithoutDefaultValues.LastOrDefault() &&
                        (_changeSignatureDialogViewModel._parametersWithDefaultValues.Any() || _changeSignatureDialogViewModel._paramsParameter != null))
                    {
                        return true;
                    }

                    if (this == _changeSignatureDialogViewModel._parametersWithDefaultValues.LastOrDefault() &&
                        _changeSignatureDialogViewModel._paramsParameter != null)
                    {
                        return true;
                    }

                    return false;
                }
            }

            public bool IsRemoved { get; set; }
        }
    }
}
