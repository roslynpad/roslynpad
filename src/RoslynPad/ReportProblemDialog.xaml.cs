using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Avalon.Windows.Controls;
using RoslynPad.UI;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for ReportProblemDialog.xaml
    /// </summary>
    [Export(typeof(IReportProblemDialog))]
    internal partial class ReportProblemDialog : IReportProblemDialog
    {
        private readonly ITelemetryProvider _telemetryProvider;
        private InlineModalDialog _dialog;

        [ImportingConstructor]
        public ReportProblemDialog(ITelemetryProvider telemetryProvider)
        {
            _telemetryProvider = telemetryProvider;
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
                await _telemetryProvider.SubmitFeedback(FeedbackText.Text, Email.Text).ConfigureAwait(true);
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Process.Start("https://github.com/aelij/RoslynPad/issues"));
        }
    }
}
