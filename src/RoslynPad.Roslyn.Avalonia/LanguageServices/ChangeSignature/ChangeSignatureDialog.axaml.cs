using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Composition;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature;

[Export(typeof(IChangeSignatureDialog))]
internal partial class ChangeSignatureDialog : Window, IChangeSignatureDialog
{
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

    private static readonly TaggedTextToTextBlockConverter s_taggedTextConverter = new();

    public ChangeSignatureDialog()
    {
        MoveUpCommand = new SimpleCommand(() => MoveUp_Click(null, null!));
        MoveDownCommand = new SimpleCommand(() => MoveDown_Click(null, null!));
        ToggleRemovedCommand = new SimpleCommand(ToggleRemovedState);

        InitializeComponent();

        Members.SelectionChanged += Members_SelectionChanged;
        Opened += (_, _) => Members.Focus();
    }

    private void Members_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.SelectedIndex = Members.SelectedIndex >= 0 ? Members.SelectedIndex : null;
        }
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (_viewModel == null) return;
        UpButton.IsEnabled = _viewModel.CanMoveUp;
        DownButton.IsEnabled = _viewModel.CanMoveDown;
        RemoveButton.IsEnabled = _viewModel.CanRemove;
        RestoreButton.IsEnabled = _viewModel.CanRestore;
        OkButton.IsEnabled = _viewModel.IsOkButtonEnabled;
        SignatureScroller.Content = s_taggedTextConverter.Convert(
            _viewModel.SignatureDisplay, typeof(object), null!, System.Globalization.CultureInfo.CurrentCulture);
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
        int oldSelectedIndex = Members.SelectedIndex;
        if (oldSelectedIndex >= 0)
        {
            _viewModel.MoveUp();
            RefreshItemsSource();
            Members.SelectedIndex = oldSelectedIndex - 1;
        }
        UpdateButtonStates();
        Members.Focus();
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not { CanMoveDown: true }) return;
        int oldSelectedIndex = Members.SelectedIndex;
        if (oldSelectedIndex >= 0)
        {
            _viewModel.MoveDown();
            RefreshItemsSource();
            Members.SelectedIndex = oldSelectedIndex + 1;
        }
        UpdateButtonStates();
        Members.Focus();
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { CanRemove: true })
        {
            _viewModel.Remove();
            RefreshItemsSource();
        }
        UpdateButtonStates();
        Members.Focus();
    }

    private void Restore_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { CanRestore: true })
        {
            _viewModel.Restore();
            RefreshItemsSource();
        }
        UpdateButtonStates();
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
        RefreshItemsSource();
        UpdateButtonStates();
        Members.Focus();
    }

    private void RefreshItemsSource()
    {
        if (_viewModel == null) return;
        var selectedIndex = Members.SelectedIndex;
        Members.ItemsSource = null;
        Members.ItemsSource = _viewModel.AllParameters;
        Members.SelectedIndex = selectedIndex;
    }

    public object ViewModel
    {
        get => DataContext ?? throw new InvalidOperationException("DataContext is null");
        set
        {
            _viewModel = (ChangeSignatureDialogViewModel)value;
            Members.ItemsSource = _viewModel.AllParameters;
            Members.SelectedIndex = _viewModel.GetStartingSelectionIndex();
            OkButton.IsEnabled = false;
            UpdateButtonStates();
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

