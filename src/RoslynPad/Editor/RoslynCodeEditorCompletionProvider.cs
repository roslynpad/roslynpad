using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.CodeAnalysis;
using RoslynPad.Annotations;
using RoslynPad.Formatting;
using RoslynPad.Roslyn;

namespace RoslynPad.Editor
{
    internal sealed class RoslynOverloadProvider : IOverloadProvider
    {
        private readonly SignatureHelpItems _signatureHelp;

        public RoslynOverloadProvider(SignatureHelpItems signatureHelp)
        {
            _signatureHelp = signatureHelp;
            SelectItem();
        }

        private int _selectedIndex;
        private SignatureHelpItem _item;
        private object _currentHeader;
        private object _currentContent;
        private string _currentIndexText;

        public event PropertyChangedEventHandler PropertyChanged;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value == _selectedIndex) return;
                SelectItem();
                _selectedIndex = value;
                OnPropertyChanged();
            }
        }

        private void SelectItem()
        {
            _item = _signatureHelp.Items[_selectedIndex];
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    ToTextBlock(_item.PrefixDisplayParts),
                }
            };
            if (_item.Parameters != null)
            {
                for (var index = 0; index < _item.Parameters.Length; index++)
                {
                    var param = _item.Parameters[index];
                    panel.Children.Add(ToTextBlock(param.DisplayParts));
                    if (index != _item.Parameters.Length - 1)
                    {
                        panel.Children.Add(ToTextBlock(_item.SeparatorDisplayParts));
                    }
                }
            }
            panel.Children.Add(ToTextBlock(_item.SuffixDisplayParts));
            CurrentHeader = panel;
            CurrentContent = ToTextBlock(_item.DocumentationFactory(CancellationToken.None));
        }

        private static TextBlock ToTextBlock(IEnumerable<SymbolDisplayPart> parts)
        {
            if (parts == null) return new TextBlock();
            return parts.ToTextBlock();
        }

        public int Count => _signatureHelp.Items.Count;

        // ReSharper disable once UnusedMember.Local
        public string CurrentIndexText
        {
            get { return _currentIndexText; }
            private set
            {
                if (value == _currentIndexText) return;
                _currentIndexText = value;
                OnPropertyChanged();
            }
        }

        public object CurrentHeader
        {
            get { return _currentHeader; }
            private set
            {
                if (Equals(value, _currentHeader)) return;
                _currentHeader = value;
                OnPropertyChanged();
            }
        }

        public object CurrentContent
        {
            get { return _currentContent; }
            private set
            {
                if (Equals(value, _currentContent)) return;
                _currentContent = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RoslynCodeEditorCompletionProvider : ICodeEditorCompletionProvider
    {
        private readonly InteractiveManager _interactiveManager;

        public RoslynCodeEditorCompletionProvider(InteractiveManager interactiveManager)
        {
            _interactiveManager = interactiveManager;
        }

        public async Task<CompletionResult> GetCompletionData(int position, char? triggerChar, bool useSignatureHelp)
        {
            IList<ICompletionDataEx> completionData = null;
            IOverloadProvider overloadProvider = null;
            bool? isCompletion = null;

            if (useSignatureHelp || triggerChar != null)
            {
                var isSignatureHelp = useSignatureHelp || await _interactiveManager.IsSignatureHelpTriggerCharacter(position - 1).ConfigureAwait(false);
                if (isSignatureHelp)
                {
                    var signatureHelp = await _interactiveManager.GetSignatureHelp(
                        new SignatureHelpTriggerInfo(
                            useSignatureHelp
                                ? SignatureHelpTriggerReason.InvokeSignatureHelpCommand
                                : SignatureHelpTriggerReason.TypeCharCommand, triggerChar), position)
                        .ConfigureAwait(false);
                    if (signatureHelp != null)
                    {
                        overloadProvider = new RoslynOverloadProvider(signatureHelp);
                    }
                }
                else
                {
                    isCompletion = await _interactiveManager.IsCompletionTriggerCharacter(position - 1).ConfigureAwait(false);
                }
            }

            if (overloadProvider == null && isCompletion != false)
            {
                var data = await _interactiveManager.GetCompletion(
                    triggerChar != null
                        ? CompletionTriggerInfo.CreateTypeCharTriggerInfo(triggerChar.Value)
                        : CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(),
                    position).ConfigureAwait(false);
                completionData = data?.Items.Select(item => new AvalonEditCompletionData(item)).ToArray<ICompletionDataEx>() 
                    ?? (IList<ICompletionDataEx>) new List<ICompletionDataEx>();
            }

            return new CompletionResult(completionData, overloadProvider);
        }
    }
}