using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface
{
    internal partial class ExtractInterfaceDialog
    {
        private readonly ExtractInterfaceDialogViewModel _viewModel;
        
        public string ExtractInterfaceDialogTitle => "Extract Interface";
        public string NewInterfaceName => "New Interface Name";
        public string GeneratedName => "Generated Name";
        public string NewFileName => "New File Name";
        public string SelectPublicMembersToFormInterface => "Select Public Members To Form Interface";
        public string SelectAll => "Select All";
        public string DeselectAll => "Deselect All";
        // ReSharper disable once InconsistentNaming
        public string OK => "OK";
        public string Cancel => "Cancel";

        internal ExtractInterfaceDialog(ExtractInterfaceDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            SetCommandBindings();

            InitializeComponent();
            DataContext = viewModel;

            Loaded += ExtractInterfaceDialog_Loaded;
            IsVisibleChanged += ExtractInterfaceDialog_IsVisibleChanged;
        }

        private void ExtractInterfaceDialog_Loaded(object sender, RoutedEventArgs e)
        {
            interfaceNameTextBox.Focus();
            interfaceNameTextBox.SelectAll();
        }

        private void ExtractInterfaceDialog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                IsVisibleChanged -= ExtractInterfaceDialog_IsVisibleChanged;
            }
        }

        private void SetCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "SelectAllClickCommand",
                    typeof(ExtractInterfaceDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.S, ModifierKeys.Alt) })),
                Select_All_Click));

            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "DeselectAllClickCommand",
                    typeof(ExtractInterfaceDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.D, ModifierKeys.Alt) })),
                Deselect_All_Click));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Select_All_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectAll();
        }

        private void Deselect_All_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeselectAll();
        }

        private void SelectAllInTextBox(object sender, RoutedEventArgs e)
        {
            TextBox textbox = e.OriginalSource as TextBox;
            if (textbox != null && Mouse.LeftButton == MouseButtonState.Released)
            {
                textbox.SelectAll();
            }
        }

        private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void OnListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void ToggleCheckSelection()
        {
            var selectedItems = Members.SelectedItems.OfType<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>().ToArray();
            var allChecked = selectedItems.All(m => m.IsChecked);
            foreach (var item in selectedItems)
            {
                item.IsChecked = !allChecked;
            }
        }
    }
}
