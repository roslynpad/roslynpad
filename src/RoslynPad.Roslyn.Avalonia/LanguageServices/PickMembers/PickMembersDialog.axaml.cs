using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Composition;

namespace RoslynPad.Roslyn.LanguageServices.PickMembers;

[Export(typeof(IPickMembersDialog))]
internal partial class PickMembersDialog : Window, IPickMembersDialog
{
    private PickMembersDialogViewModel? _viewModel;

    public string PickMembersDialogTitle => "Pick members";
    public string SelectAll => "Select All";
    public string DeselectAll => "Deselect All";
    public string OK => "OK";
    public string Cancel => "Cancel";

    public string? DialogTitle { get; private set; }

    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    [ImportingConstructor]
    public PickMembersDialog()
    {
        MoveUpCommand = new SimpleCommand(() => MoveUp_Click(null, null!));
        MoveDownCommand = new SimpleCommand(() => MoveDown_Click(null, null!));

        InitializeComponent();

        Members.SelectionChanged += Members_SelectionChanged;
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
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
        => Close(true);

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => Close(false);

    private void Select_All_Click(object? sender, RoutedEventArgs e)
        => _viewModel?.SelectAll();

    private void Deselect_All_Click(object? sender, RoutedEventArgs e)
        => _viewModel?.DeselectAll();

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

    private void RefreshItemsSource()
    {
        if (_viewModel == null) return;
        var selectedIndex = Members.SelectedIndex;
        Members.ItemsSource = null;
        Members.ItemsSource = _viewModel.MemberContainers;
        Members.SelectedIndex = selectedIndex;
    }

    private void OnListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
        {
            ToggleCheckSelection();
            e.Handled = true;
        }
    }

    private void OnListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        ToggleCheckSelection();
        e.Handled = true;
    }

    private void ToggleCheckSelection()
    {
        var selectedItems = Members.SelectedItems?.OfType<PickMembersDialogViewModel.MemberSymbolViewModel>().ToArray();
        if (selectedItems == null) return;
        var allChecked = selectedItems.All(m => m.IsChecked);
        foreach (var item in selectedItems)
        {
            item.IsChecked = !allChecked;
        }
    }

    string? IPickMembersDialog.Title
    {
        get => DialogTitle;
        set
        {
            DialogTitle = value;
            Title = value ?? PickMembersDialogTitle;
        }
    }

    public object ViewModel
    {
        get => DataContext ?? throw new InvalidOperationException("DataContext is null");
        set
        {
            _viewModel = (PickMembersDialogViewModel)value;
            Members.ItemsSource = _viewModel.MemberContainers;
            OptionsControl.ItemsSource = _viewModel.Options;
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

