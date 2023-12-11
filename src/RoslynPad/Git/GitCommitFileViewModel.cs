using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using RoslynPad.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad
{
    public class GitCommitFileViewModel : IOpenDocumentViewModel, IDisposable
    {
        public GitCommitFileViewModel(string path, Blob blob, MainViewModel model)
        {
            FilePath = path;
            Blob = blob;
            MainViewModel = model;
            DocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());
        }
        public string FilePath { get;}
        public MainViewModel MainViewModel { get; }
        public Blob Blob { get; }
        public DocumentViewModel? Document => null;
        public DocumentId DocumentId { get; }

        public string Title { get; set; }="";

        public bool IsDirty => false;

        public Task AutoSaveAsync()
        {
            return Task.Run(() =>
            {

            });
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public Task<SaveResult> SaveAsync(bool promptSave)
        {
            return Task<SaveResult>.Run(() =>
            {
                return SaveResult.DoNotSave;
            });
        }
    }
}
