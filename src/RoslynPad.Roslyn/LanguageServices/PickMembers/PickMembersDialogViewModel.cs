// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PickMembers;

namespace RoslynPad.Roslyn.LanguageServices.PickMembers
{
    internal class PickMembersDialogViewModel : NotificationObject
    {
        public List<MemberSymbolViewModel> MemberContainers { get; set; }
        public List<OptionViewModel> Options { get; set; }

        internal PickMembersDialogViewModel(
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options)
        {
            MemberContainers = members.Select(m => new MemberSymbolViewModel(m)).ToList();
            Options = options.Select(o => new OptionViewModel(o)).ToList();
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
                OnPropertyChanged(nameof(MoveUpAutomationText));
                OnPropertyChanged(nameof(CanMoveDown));
                OnPropertyChanged(nameof(MoveDownAutomationText));
            }
        }

        public string MoveUpAutomationText
        {
            get
            {
                if (!CanMoveUp || SelectedIndex == null)
                {
                    return string.Empty;
                }

                return string.Format("Move {0} below {1}", MemberContainers[SelectedIndex.Value].MemberAutomationText, MemberContainers[SelectedIndex.Value - 1].MemberAutomationText);
            }
        }

        public string MoveDownAutomationText
        {
            get
            {
                if (!CanMoveDown || SelectedIndex == null)
                {
                    return string.Empty;
                }

                return string.Format("Move {0} below {1}", MemberContainers[SelectedIndex.Value].MemberAutomationText, MemberContainers[SelectedIndex.Value + 1].MemberAutomationText);
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
                return index > 0;
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
                return index < MemberContainers.Count - 1;
            }
        }

        internal void MoveUp()
        {
            Debug.Assert(CanMoveUp);
            if (SelectedIndex == null) return;

            var index = SelectedIndex.Value;
            Move(MemberContainers, index, delta: -1);
        }

        internal void MoveDown()
        {
            Debug.Assert(CanMoveDown);
            if (SelectedIndex == null) return;

            var index = SelectedIndex.Value;
            Move(MemberContainers, index, delta: 1);
        }

        private void Move(List<MemberSymbolViewModel> list, int index, int delta)
        {
            var param = list[index];
            list.RemoveAt(index);
            list.Insert(index + delta, param);

            SelectedIndex += delta;
        }

        internal class MemberSymbolViewModel : NotificationObject
        {
            public ISymbol MemberSymbol { get; }

            private static SymbolDisplayFormat s_memberDisplayFormat = new SymbolDisplayFormat(
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
                set { SetProperty(ref _isChecked, value); }
            }

            public string MemberName => MemberSymbol.ToDisplayString(s_memberDisplayFormat);

            public Completion.Glyph Glyph => MemberSymbol.GetGlyph();

            public string MemberAutomationText => MemberSymbol.Kind + " " + MemberName;
        }

        internal class OptionViewModel : NotificationObject
        {
            public PickMembersOption Option { get; }

            public string Title { get; }

            public OptionViewModel(PickMembersOption option)
            {
                Option = option;
                Title = option.Title;
                IsChecked = option.Value;
            }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;

                set
                {
                    Option.Value = value;
                    SetProperty(ref _isChecked, value);
                }
            }

            public string MemberAutomationText => Option.Title;
        }
    }
}
