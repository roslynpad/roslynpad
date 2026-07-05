using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Morgania.CodeAnalysis.Editor.LanguageServices.ChangeSignature;

[Export(typeof(IChangeSignatureDialog))]
internal partial class ChangeSignatureDialog : Window, IChangeSignatureDialog
{
    private readonly IClassificationFormatMap? _classificationFormatMap;
    private readonly ClassificationTypeMap? _classificationTypeMap;
    private ChangeSignatureDialogViewModel? _viewModel;

    public string ChangeSignatureDialogTitle => "Change Signature";
    public string Parameters => "Parameters";
    public string Remove => "Remove";
    public string Restore => "Restore";
    public string OK => "OK";
    public string Cancel => "Cancel";

    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ToggleRemovedCommand { get; }

    // Required by the XAML compiler; instantiation goes through the importing constructor.
    public ChangeSignatureDialog()
    {
        MoveUpCommand = new SimpleCommand(() => MoveUp_Click(null, null!));
        MoveDownCommand = new SimpleCommand(() => MoveDown_Click(null, null!));
        ToggleRemovedCommand = new SimpleCommand(ToggleRemovedState);

        InitializeComponent();

        Opened += (_, _) => Members.Focus();
    }

    [ImportingConstructor]
    public ChangeSignatureDialog(IClassificationFormatMapService classificationFormatMapService, ClassificationTypeMap classificationTypeMap)
        : this()
    {
        // The "text" appearance category is the map the editor host themes (colors and font),
        // so the signature preview renders exactly like the code in the editor.
        _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
        _classificationTypeMap = classificationTypeMap;
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.TrySubmit() == true)
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not { CanMoveUp: true }) return;
        _viewModel.MoveUp();
        Members.Focus();
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not { CanMoveDown: true }) return;
        _viewModel.MoveDown();
        Members.Focus();
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { CanRemove: true })
        {
            _viewModel.Remove();
        }
        Members.Focus();
    }

    private void Restore_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { CanRestore: true })
        {
            _viewModel.Restore();
        }
        Members.Focus();
    }

    private void ToggleRemovedState()
    {
        if (_viewModel == null) return;
        if (_viewModel.CanRemove)
        {
            _viewModel.Remove();
        }
        else if (_viewModel.CanRestore)
        {
            _viewModel.Restore();
        }
        Members.Focus();
    }

    public object ViewModel
    {
        get => DataContext ?? throw new InvalidOperationException("DataContext is null");
        set
        {
            DataContext = value;
            _viewModel = (ChangeSignatureDialogViewModel)value;
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChangeSignatureDialogViewModel.SignatureDisplay))
                {
                    UpdateSignaturePreview();
                }
            };
            UpdateSignaturePreview();
            Members.SelectedIndex = _viewModel.GetStartingSelectionIndex();
        }
    }

    private void UpdateSignaturePreview()
    {
        if (_viewModel is { } viewModel && _classificationFormatMap is { } formatMap && _classificationTypeMap is { } typeMap)
        {
            SignatureScroller.Content = viewModel.SignatureDisplay.ToTextBlock(formatMap, typeMap);
        }
    }

    bool? IRoslynDialog.Show()
    {
        return this.ShowDialogSync();
    }

    private class SimpleCommand(Action execute) : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}

