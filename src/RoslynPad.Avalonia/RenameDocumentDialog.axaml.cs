using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for RenameDocumentDialog.axaml
/// </summary>
[Export(typeof(IRenameDocumentDialog))]
partial class RenameDocumentDialog : UserControl, IRenameDocumentDialog, INotifyPropertyChanged
{
    private static readonly char[] s_invalidFileChars = Path.GetInvalidFileNameChars();

    private bool _shouldRename;

    static RenameDocumentDialog()
    {
        DocumentNameProperty.Changed.AddClassHandler<RenameDocumentDialog, string?>((sender, args) => sender.SetRenameButtonStatus(args.NewValue.Value));
    }

    public RenameDocumentDialog()
    {
        DataContext = this;
        InitializeComponent();

        Loaded += OnLoaded;
    }

    public void Initialize(string documentName)
    {
        DocumentName = documentName;
    }

    private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        DocumentTextBox.Focus();
        DocumentTextBox.SelectAll();
        SetRenameButtonStatus(DocumentName);
    }

    private void SetRenameButtonStatus(string? documentName)
    {
        RenameButton.IsEnabled = !string.IsNullOrWhiteSpace(documentName) && IsValidDocumentName(documentName);
    }

    private static bool IsValidDocumentName(string? s) => s?.IndexOfAny(s_invalidFileChars) is null or < 0;

    public static readonly DirectProperty<RenameDocumentDialog, bool> ShouldRenameProperty =
        AvaloniaProperty.RegisterDirect<RenameDocumentDialog, bool>(nameof(ShouldRename),
            d => d._shouldRename, (d, value) => d._shouldRename = value);

    public static readonly StyledProperty<string?> DocumentNameProperty =
        AvaloniaProperty.Register<RenameDocumentDialog, string?>(nameof(DocumentName),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            enableDataValidation: true,
            validate: IsValidDocumentName);

    public bool ShouldRename
    {
        get => _shouldRename;
        private set => SetAndRaise(ShouldRenameProperty, ref _shouldRename, value);
    }

    public string? DocumentName
    {
        get => GetValue(DocumentNameProperty);
        set => SetValue(DocumentNameProperty, value);
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

    private void DocumentText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && RenameButton.IsEnabled)
        {
            ShouldRename = true;
            Close();
        }
    }
}
