using Avalon.Windows.Controls;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for RenameDocumentDialog.xaml
/// </summary>
[Export(typeof(IRenameDocumentDialog))]
internal partial class RenameDocumentDialog : INotifyPropertyChanged, IRenameDocumentDialog
{
    private string? _documentName;
    private InlineModalDialog? _dialog;

    public RenameDocumentDialog()
    {
        DataContext = this;
        InitializeComponent();
    }

    public void Initialize(string documentName)
    {
        Loaded += (sender, args) =>
        {
            DocumentTextBox.Focus();
            DocumentTextBox.SelectAll();
        };
        DocumentName = documentName;
    }

    public bool ShouldRename { get; private set; }

    public string? DocumentName
    {
        get => _documentName;
        set
        {
            SetProperty(ref _documentName, value);
            SetRenameButtonStatus();
        }
    }

    private void SetRenameButtonStatus()
    {
        RenameButton.IsEnabled = !string.IsNullOrWhiteSpace(DocumentName) && IsValidDocumentName(DocumentName);
    }

    private static bool IsValidDocumentName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var invalidChars = Path.GetInvalidFileNameChars();
        return name.IndexOfAny(invalidChars) < 0;
    }

    private void DocumentName_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in e.Changes)
        {
            if (c.AddedLength == 0) continue;
            textBox.Select(c.Offset, c.AddedLength);
            var filteredText = invalidChars.Aggregate(textBox.SelectedText,
                (current, invalidChar) => current.Replace(invalidChar.ToString(), string.Empty));
            if (textBox.SelectedText != filteredText)
            {
                textBox.SelectedText = filteredText;
            }
            textBox.Select(c.Offset + c.AddedLength, 0);
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

    public Task ShowAsync()
    {
        _dialog = new InlineModalDialog
        {
            Owner = Application.Current.MainWindow,
            Content = this
        };
        _dialog.Show();
        return Task.CompletedTask;
    }

    public void Close()
    {
        _dialog?.Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }
}
