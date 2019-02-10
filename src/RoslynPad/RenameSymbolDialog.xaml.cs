using System.ComponentModel;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalon.Windows.Controls;
using RoslynPad.Annotations;
using RoslynPad.UI;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for RenameSymbolDialog.xaml
    /// </summary>
    [Export(typeof(IRenameSymbolDialog))]
    public partial class RenameSymbolDialog : INotifyPropertyChanged, IRenameSymbolDialog
    {
        private static readonly Regex _identifierRegex = new Regex(@"^(?:((?!\d)\w+(?:\.(?!\d)\w+)*)\.)?((?!\d)\w+)$");

        private string? _symbolName;
        private InlineModalDialog? _dialog;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public RenameSymbolDialog()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            DataContext = this;
            InitializeComponent();

        }

        public void Initialize(string symbolName)
        {
            Loaded += (sender, args) =>
            {
                SymbolTextBox.Focus();
                SymbolTextBox.SelectionStart = symbolName.Length;
            };
            SymbolName = symbolName;
        }

        public bool ShouldRename { get; private set; }

        public string? SymbolName
        {
            get => _symbolName;
            set
            {
                _symbolName = value;
                OnPropertyChanged();
                RenameButton.IsEnabled = value != null && _identifierRegex.IsMatch(value);
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

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            ShouldRename = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SymbolText_KeyDown(object sender, KeyEventArgs e)
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
