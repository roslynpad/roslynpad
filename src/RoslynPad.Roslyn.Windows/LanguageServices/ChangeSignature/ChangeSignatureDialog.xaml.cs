using System;
using System.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    /// <summary>
    /// Interaction logic for ChangeSignatureDialog.xaml
    /// </summary>
    [Export(typeof(IChangeSignatureDialog))]
    internal partial class ChangeSignatureDialog : IChangeSignatureDialog
    {
        private ChangeSignatureDialogViewModel _viewModel;

        // Expose localized strings for binding
        public string ChangeSignatureDialogTitle => "Change Signature";
        public string Parameters => "Parameters";
        public string PreviewMethodSignature => "Preview Method Signature";
        public string PreviewReferenceChanges => "PreviewReferenceChanges";
        public string Remove => "Remove";
        public string Restore => "Restore";
        // ReSharper disable once InconsistentNaming
        public string OK => "OK";
        public string Cancel => "Cancel";

        public Brush ParameterText { get; }
        public Brush RemovedParameterText { get; }
        public Brush DisabledParameterForeground { get; }
        public Brush DisabledParameterBackground { get; }
        public Brush StrikethroughBrush { get; }


        // Use C# Reorder Parameters helpTopic for C# and VB.
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public ChangeSignatureDialog()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            InitializeComponent();

            // Set these headers explicitly because binding to DataGridTextColumn.Header is not
            // supported.
            modifierHeader.Header = "Modifier";
            defaultHeader.Header = "Default";
            typeHeader.Header = "Type";
            parameterHeader.Header = "Parameter";

            ParameterText = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E));
            RemovedParameterText = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Colors.Gray);
            DisabledParameterBackground = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xDF, 0xE7, 0xF3));
            DisabledParameterForeground = SystemParameters.HighContrast ? SystemColors.GrayTextBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xA2, 0xA4, 0xA5));
            Members.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            StrikethroughBrush = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Colors.Red);

            Loaded += ChangeSignatureDialog_Loaded;
            IsVisibleChanged += ChangeSignatureDialog_IsVisibleChanged;
        }

        private void ChangeSignatureDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Members.Focus();
        }

        private void ChangeSignatureDialog_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                IsVisibleChanged -= ChangeSignatureDialog_IsVisibleChanged;
            }
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

        private void MoveUp_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveUp && oldSelectedIndex >= 0)
            {
                _viewModel.MoveUp();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex - 1;
            }

            SetFocusToSelectedRow();
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveDown && oldSelectedIndex >= 0)
            {
                _viewModel.MoveDown();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex + 1;
            }

            SetFocusToSelectedRow();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanRemove)
            {
                _viewModel.Remove();
                Members.Items.Refresh();
            }

            SetFocusToSelectedRow();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanRestore)
            {
                _viewModel.Restore();
                Members.Items.Refresh();
            }

            SetFocusToSelectedRow();
        }

        private void SetFocusToSelectedRow()
        {
            if (Members.SelectedIndex >= 0)
            {
                var row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as DataGridRow;
                if (row == null)
                {
                    Members.ScrollIntoView(Members.SelectedItem);
                    row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as DataGridRow;
                }

                if (row != null)
                {
                    FocusRow(row);
                }
            }
        }

        private void FocusRow(DataGridRow row)
        {
            // TODO
            //DataGridCell cell = row.FindDescendant<DataGridCell>();
            //if (cell != null)
            //{
            //    cell.Focus();
            //}
        }

        private void MoveSelectionUp_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (oldSelectedIndex > 0)
            {
                var potentialNewSelectedParameter = Members.Items[oldSelectedIndex - 1] as ChangeSignatureDialogViewModel.ParameterViewModel;
                if (potentialNewSelectedParameter?.IsDisabled == false)
                {
                    Members.SelectedIndex = oldSelectedIndex - 1;
                }
            }

            SetFocusToSelectedRow();
        }

        private void MoveSelectionDown_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (oldSelectedIndex >= 0 && oldSelectedIndex < Members.Items.Count - 1)
            {
                Members.SelectedIndex = oldSelectedIndex + 1;
            }

            SetFocusToSelectedRow();
        }

        private void Members_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Members.SelectedIndex == -1)
            {
                Members.SelectedIndex = _viewModel.GetStartingSelectionIndex();
            }

            SetFocusToSelectedRow();
        }

        private void ToggleRemovedState(object sender, ExecutedRoutedEventArgs e)
        {
            if (_viewModel.CanRemove)
            {
                _viewModel.Remove();
            }
            else if (_viewModel.CanRestore)
            {
                _viewModel.Restore();
            }

            Members.Items.Refresh();
            SetFocusToSelectedRow();
        }

        public object ViewModel
        {
            get => DataContext;
            set
            {
                DataContext = value;
                _viewModel = (ChangeSignatureDialogViewModel)value;
            }
        }

        bool? IRoslynDialog.Show()
        {
            this.SetOwnerToActive();
            return ShowDialog();
        }
    }
}
