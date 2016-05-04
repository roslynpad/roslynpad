using System.Windows;
using System.Windows.Controls;
using Avalon.Windows.Controls;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for ReportProblemDialog.xaml
    /// </summary>
    internal partial class ReportProblemDialog
    {
        private readonly MainViewModel _mainViewModel;
        private InlineModalDialog _dialog;

        public ReportProblemDialog(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            FeedbackText.Focus();
        }

        public void Show()
        {
            _dialog = new InlineModalDialog
            {
                Owner = Application.Current.MainWindow,
                Content = this
            };
            _dialog.Show();
        }

        public void Close()
        {
            _dialog?.Close();
        }

        private async void Submit_OnClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            BusyIndicator.IsIndeterminate = true;
            try
            {
                await _mainViewModel.SubmitFeedback(FeedbackText.Text, Email.Text).ConfigureAwait(true);
                Close();
            }
            finally
            {
                IsEnabled = true;
                BusyIndicator.IsIndeterminate = false;
            }
        }

        private void FeedbackText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Submit.IsEnabled = !string.IsNullOrWhiteSpace(((TextBox)sender).Text);
        }
    }
}
