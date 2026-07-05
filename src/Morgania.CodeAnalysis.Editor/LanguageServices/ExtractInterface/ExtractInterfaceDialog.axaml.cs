using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Composition;

namespace Morgania.CodeAnalysis.Editor.LanguageServices.ExtractInterface;

[Export(typeof(IExtractInterfaceDialog))]
internal partial class ExtractInterfaceDialog : Window, IExtractInterfaceDialog
{
    private ExtractInterfaceDialogViewModel? _viewModel;

    public string ExtractInterfaceDialogTitle => "Extract Interface";
    public string NewInterfaceName => "New Interface Name";
    public string GeneratedName => "Generated Name";
    public string NewFileName => "New File Name";
    public string DestinationLabel => "Add to";
    public string CurrentFileLabel => "Current file";
    public string NewFileLabel => "New file";
    public string SelectPublicMembersToFormInterface => "Select Public Members To Form Interface";
    public string SelectAll => "Select All";
    public string DeselectAll => "Deselect All";
    public string OK => "OK";
    public string Cancel => "Cancel";

    public ExtractInterfaceDialog()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            InterfaceNameTextBox.Focus();
            InterfaceNameTextBox.SelectAll();
        };
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

    private void Select_All_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.SelectAll();
    }

    private void Deselect_All_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.DeselectAll();
    }

    private void SelectAllInTextBox(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
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
        var selectedItems = Members.SelectedItems?.OfType<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>().ToArray();
        if (selectedItems == null) return;
        var allChecked = selectedItems.All(m => m.IsChecked);
        foreach (var item in selectedItems)
        {
            item.IsChecked = !allChecked;
        }
    }

    public object ViewModel
    {
        get => DataContext ?? throw new InvalidOperationException("DataContext is null");
        set
        {
            DataContext = value;
            _viewModel = (ExtractInterfaceDialogViewModel)value;
        }
    }

    bool? IRoslynDialog.Show()
    {
        return this.ShowDialogSync();
    }
}

