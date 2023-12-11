using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using RoslynPad.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad
{
    public class GitChangesViewModel : IOpenDocumentViewModel, IDisposable
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public GitChangesViewModel(string path, string status, bool isFolder=false)
        {
            Path = path;
            Status = status;
            IsFolder = isFolder;
            Name = System.IO.Path.GetFileName(path);
            DocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());
        }
        public MainViewModel MainViewModel { get; set; }
        public DocumentViewModel? Document => null;
        public DocumentId DocumentId { get; }

        public string Title { get;  set; } = "";

        public bool IsDirty => false;
        public bool IsFolder { get; private set; }
        public string Name { get; private set; }
        public string Path { get; set; }
        public string Status { get; set; }
        public ObservableCollection<GitChangesViewModel> Children
        {
            get;
        } = new ObservableCollection<GitChangesViewModel>();

        public Task AutoSaveAsync()
        {
            return Task.Run(() =>
            {

            });
        }

        public void Close()
        {
        }

        public Task<SaveResult> SaveAsync(bool promptSave)
        {
            return Task<SaveResult>.Run(() =>
            {
                return SaveResult.DoNotSave;
            });
        }
        public void Dispose()
        {
            foreach (var i in Children)
                i.Dispose();
            Children.Clear();
        }
    }
}
