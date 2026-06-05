using Avalon.Windows.Controls;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for SaveDocumentDialog.xaml
/// </summary>
[Export(typeof(ISaveDocumentDialog))]
internal partial class SaveDocumentDialog : ISaveDocumentDialog, INotifyPropertyChanged
{
    private InlineModalDialog? _dialog;

    public SaveDocumentDialog()
    {
        DataContext = this;
        InitializeComponent();
        DocumentName = null;
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
        SetSaveButtonStatus();
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

    public string? DocumentName
    {
        get;
        set
        {
            SetProperty(ref field, value);
            SetSaveButtonStatus();
        }
    }

    private void SetSaveButtonStatus()
    {
        SaveButton.IsEnabled = !AllowNameEdit || !string.IsNullOrWhiteSpace(DocumentName);
    }

    public SaveResult Result
    {
        get; private set => SetProperty(ref field, value);
    }

    public bool AllowNameEdit
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowDoNotSave
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? FilePath
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public Func<string, string>? FilePathFactory { get; set; }

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

    private void PerformSave()
    {
        if (AllowNameEdit && !string.IsNullOrEmpty(DocumentName))
        {
            FilePath = FilePathFactory?.Invoke(DocumentName) ?? throw new InvalidOperationException();
            if (File.Exists(FilePath))
            {
                SaveButton.Visibility = Visibility.Collapsed;
                OverwriteButton.Visibility = Visibility.Visible;
                DocumentTextBox.IsEnabled = false;
                _ = Dispatcher.InvokeAsync(OverwriteButton.Focus);
            }
            else
            {
                Result = SaveResult.Save;
                Close();
            }
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

    private void DontSave_Click(object? sender, RoutedEventArgs e)
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
