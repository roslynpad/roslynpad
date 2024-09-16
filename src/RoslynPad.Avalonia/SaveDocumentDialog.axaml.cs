using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DialogHostAvalonia;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for SaveDocumentDialog.xaml
/// </summary>
[Export(typeof(ISaveDocumentDialog))]
partial class SaveDocumentDialog : UserControl, ISaveDocumentDialog, INotifyPropertyChanged
{
    private const string HostIdentifier = "Main";

    private static readonly char[] s_invalidFileChars = Path.GetInvalidFileNameChars();

    private bool _showDoNotSave;
    private bool _allowNameEdit;
    private string? _filePath;
    private SaveResult _result;

    static SaveDocumentDialog()
    {
        DocumentNameProperty.Changed.AddClassHandler<SaveDocumentDialog, string?>((sender, args) => sender.SetSaveButtonStatus(args.NewValue.Value));
    }

    public SaveDocumentDialog()
    {
        DataContext = this;
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (AllowNameEdit)
        {
            DocumentTextBox.Focus();
        }
        else
        {
            SaveButton.Focus();
        }

        SetSaveButtonStatus(documentName: null);
    }

    private void SetSaveButtonStatus(string? documentName)
    {
        SaveButton.IsEnabled = !AllowNameEdit || (!string.IsNullOrWhiteSpace(documentName) && IsValidDocumentName(documentName));
    }

    public static readonly DirectProperty<SaveDocumentDialog, SaveResult> ResultProperty =
        AvaloniaProperty.RegisterDirect<SaveDocumentDialog, SaveResult>(nameof(Result),
            d => d._result, (d, value) => d._result = value);

    public static readonly DirectProperty<SaveDocumentDialog, bool> AllowNameEditProperty =
        AvaloniaProperty.RegisterDirect<SaveDocumentDialog, bool>(nameof(AllowNameEdit),
            d => d._allowNameEdit, (d, value) => d._allowNameEdit = value);

    public static readonly DirectProperty<SaveDocumentDialog, bool> ShowDoNotSaveProperty =
      AvaloniaProperty.RegisterDirect<SaveDocumentDialog, bool>(nameof(ShowDoNotSave),
          d => d._showDoNotSave, (d, value) => d._showDoNotSave = value);

    public static readonly DirectProperty<SaveDocumentDialog, string?> FilePathProperty =
      AvaloniaProperty.RegisterDirect<SaveDocumentDialog, string?>(nameof(FilePath),
          d => d._filePath, (d, value) => d._filePath = value);

    public static readonly StyledProperty<string?> DocumentNameProperty =
      AvaloniaProperty.Register<SaveDocumentDialog, string?>(nameof(DocumentName),
          defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
          enableDataValidation: true,
          validate: IsValidDocumentName);

    private static bool IsValidDocumentName(string? s) => s?.IndexOfAny(s_invalidFileChars) is null or < 0;

    public SaveResult Result
    {
        get => _result;
        private set => SetAndRaise(ResultProperty, ref _result, value);
    }

    public bool AllowNameEdit
    {
        get => _allowNameEdit;
        set => SetAndRaise(AllowNameEditProperty, ref _allowNameEdit, value);
    }

    public bool ShowDoNotSave
    {
        get => _showDoNotSave;
        set => SetAndRaise(ShowDoNotSaveProperty, ref _showDoNotSave, value);
    }

    public string? FilePath
    {
        get => _filePath;
        private set => SetAndRaise(FilePathProperty, ref _filePath, value);
    }

    public string? DocumentName
    {
        get => GetValue(DocumentNameProperty);
        set => SetValue(DocumentNameProperty, value);
    }

    public Func<string, string>? FilePathFactory { get; set; }

    public async Task ShowAsync()
    {
        await DialogHost.Show(this, HostIdentifier).ConfigureAwait(true);
    }

    public void Close()
    {
        DialogHost.Close(HostIdentifier);
    }

    private void PerformSave()
    {
        if (!AllowNameEdit || string.IsNullOrEmpty(DocumentName))
        {
            Result = SaveResult.Save;
            Close();
            return;
        }

        FilePath = FilePathFactory?.Invoke(DocumentName) ?? throw new InvalidOperationException();
        if (File.Exists(FilePath))
        {
            SaveButton.IsVisible = false;
            OverwriteButton.IsVisible = false;
            DocumentTextBox.IsEnabled = false;
            Dispatcher.UIThread.InvokeAsync(() => OverwriteButton.Focus());
        }
        else
        {
            Result = SaveResult.Save;
            Close();
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

    private void Overwrite_Click(object? sender, RoutedEventArgs e)
    {
        Result = SaveResult.Save;
        Close();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        PerformSave();
    }

    private void DoNotSave_Click(object? sender, RoutedEventArgs e)
    {
        Result = SaveResult.DoNotSave;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DocumentText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SaveButton.IsEnabled)
        {
            PerformSave();
        }
    }
}
