using System.ComponentModel;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for RenameSymbolDialog.axaml
/// </summary>
[Export(typeof(IRenameSymbolDialog))]
partial class RenameSymbolDialog : UserControl, IRenameSymbolDialog, INotifyPropertyChanged
{
    private static readonly Regex s_identifierRegex = IdentifierRegex();

    private bool _shouldRename;

    static RenameSymbolDialog()
    {
        SymbolNameProperty.Changed.AddClassHandler<RenameSymbolDialog, string?>((sender, args) => sender.SetRenameButtonStatus(args.NewValue.Value));
    }

    public RenameSymbolDialog()
    {
        DataContext = this;
        InitializeComponent();
    }

    public void Initialize(string symbolName)
    {
        SymbolName = symbolName;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        SymbolTextBox.Focus();
        SymbolTextBox.SelectionStart = SymbolName?.Length ?? 0;
        SymbolTextBox.SelectionEnd = SymbolTextBox.SelectionStart;
        SetRenameButtonStatus(SymbolName);
    }

    private void SetRenameButtonStatus(string? symbolName)
    {
        RenameButton.IsEnabled = symbolName != null && s_identifierRegex.IsMatch(symbolName);
    }

    private static bool IsValidIdentifier(string? s) => s is null || s_identifierRegex.IsMatch(s);

    public static readonly DirectProperty<RenameSymbolDialog, bool> ShouldRenameProperty =
        AvaloniaProperty.RegisterDirect<RenameSymbolDialog, bool>(nameof(ShouldRename),
            d => d._shouldRename, (d, value) => d._shouldRename = value);

    public static readonly StyledProperty<string?> SymbolNameProperty =
        AvaloniaProperty.Register<RenameSymbolDialog, string?>(nameof(SymbolName),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            enableDataValidation: true,
            validate: IsValidIdentifier);

    public bool ShouldRename
    {
        get => _shouldRename;
        private set => SetAndRaise(ShouldRenameProperty, ref _shouldRename, value);
    }

    public string? SymbolName
    {
        get => GetValue(SymbolNameProperty);
        set => SetValue(SymbolNameProperty, value);
    }

    public async Task ShowAsync()
    {
        await DialogHost.Show(this, MainWindow.DialogHostIdentifier).ConfigureAwait(true);
    }

    public void Close()
    {
        if (DialogHost.IsDialogOpen(MainWindow.DialogHostIdentifier))
        {
            DialogHost.Close(MainWindow.DialogHostIdentifier);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void Rename_Click(object? sender, RoutedEventArgs e)
    {
        ShouldRename = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SymbolText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && RenameButton.IsEnabled)
        {
            ShouldRename = true;
            Close();
        }
    }

    [GeneratedRegex(@"^(?:((?!\d)\w+(?:\.(?!\d)\w+)*)\.)?((?!\d)\w+)$")]
    private static partial Regex IdentifierRegex();
}
