using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Avalon.Windows.Controls;
using RoslynPad.Annotations;
using RoslynPad.UI;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for SaveDocumentDialog.xaml
    /// </summary>
    [Export(typeof(ISaveDocumentDialog))]
    internal partial class SaveDocumentDialog : ISaveDocumentDialog, INotifyPropertyChanged
    {
        private string? _documentName;
        private bool _showDontSave;
        private InlineModalDialog _dialog;
        private bool _allowNameEdit;
        private string _filePath;
        private SaveResult _result;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public SaveDocumentDialog()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            DataContext = this;
            InitializeComponent();
            DocumentName = null;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
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

        private void DocumentName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
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
            get => _documentName;
            set
            {
                SetProperty(ref _documentName, value);
                SetSaveButtonStatus();
            }
        }

        private void SetSaveButtonStatus()
        {
            SaveButton.IsEnabled = !AllowNameEdit || !string.IsNullOrWhiteSpace(DocumentName);
        }

        public SaveResult Result
        {
            get => _result; private set => SetProperty(ref _result, value);
        }

        public bool AllowNameEdit
        {
            get => _allowNameEdit;
            set => SetProperty(ref _allowNameEdit, value);
        }

        public bool ShowDontSave
        {
            get => _showDontSave;
            set => SetProperty(ref _showDontSave, value);
        }

        public string FilePath
        {
            get => _filePath;
            private set => SetProperty(ref _filePath, value);
        }

        public Func<string, string> FilePathFactory { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotifyPropertyChangedInvocator]
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
                    Dispatcher.InvokeAsync(() => OverwriteButton.Focus());
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

        private void Overwrite_Click(object sender, RoutedEventArgs e)
        {
            Result = SaveResult.Save;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            PerformSave();
        }

        private void DontSave_Click(object sender, RoutedEventArgs e)
        {
            Result = SaveResult.DontSave;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DocumentText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SaveButton.IsEnabled)
            {
                PerformSave();
            }
        }
    }
}
