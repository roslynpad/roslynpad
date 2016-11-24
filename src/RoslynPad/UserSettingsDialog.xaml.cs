using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Avalon.Windows.Controls;
using RoslynPad.Annotations;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for UserSettingsDialog.xaml
    /// </summary>
    public partial class UserSettingsDialog : INotifyPropertyChanged
    {
        private InlineModalDialog _dialog;
        private SaveResult _result;

        private MainViewModel ParentView { get; }
        public SaveResult Result
        {
            get { return _result; }
            private set { SetProperty(ref _result, value); }
        }

        public string UserDocumentPath { get; internal set; } = GetUserDocumentPath();
        public bool UserCreateSampleFiles { get; internal set; } = Properties.Settings.Default.CreateSamples;

        internal UserSettingsDialog(MainViewModel view)
        {
            InitializeComponent();
            ParentView = view;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            DocumentPath.Text = UserDocumentPath;
            DocumentPathBrowse.Focus();

            CreateSampleFiles.IsChecked = UserCreateSampleFiles;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            PerformSave();
            RestartApplication();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        internal static string GetUserDocumentPath()
        {
            var userDefinedPath = Properties.Settings.Default.DocumentPath;
            return !string.IsNullOrEmpty(userDefinedPath) && System.IO.Directory.Exists(userDefinedPath)
                ? userDefinedPath
                : GetDefaultDocumentPath();
        }

        private static string GetDefaultDocumentPath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RoslynPad");
        }

        private void RestartApplication()
        {
            // TODO: Review use of System.Windows.Forms reference.
            System.Windows.Forms.Application.Restart();
            Environment.Exit(0);
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

        private void PerformSave()
        {
            SaveDocumentPath(UserDocumentPath);
            SaveCreateSamples(UserCreateSampleFiles);
            Result = SaveResult.Save;
        }

        private void SaveDocumentPath(string documentPath)
        {
            if (Directory.Exists(documentPath))
            {
                Properties.Settings.Default.DocumentPath = documentPath;
                Properties.Settings.Default.Save();
            }
        }

        private void SaveCreateSamples(bool createSamples)
        {
            Properties.Settings.Default.CreateSamples = createSamples;
            Properties.Settings.Default.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotifyPropertyChangedInvocator]
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        private void DocumentPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Review use of System.Windows.Forms reference.
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = GetUserDocumentPath();
            dialog.ShowNewFolderButton = false;

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                UserDocumentPath = dialog.SelectedPath;
                DocumentPath.Text = UserDocumentPath;
            }
        }

        private void CreateSampleFiles_Click(object sender, RoutedEventArgs e)
        {
            UserCreateSampleFiles = CreateSampleFiles.IsChecked.HasValue ? CreateSampleFiles.IsChecked.Value : UserCreateSampleFiles;
        }
    }
}
