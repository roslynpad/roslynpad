using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using RoslynPad.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad
{
    public class CommitItem
    {
        public string ID { get;}
        public string FullId { get; }
        public DateTime Date { get; }
        public string Author { get; }
        public string Message { get; }
        public CommitItem(string id, DateTime date, string author, string message)
        {
            ID = id.Substring(0,8);
            FullId = id;
            Date = date;
            Author = author;
            Message = message;
        }

    }
    public enum BranchHistoryType
    {
        Commit,
        File
    }
    public class GitBranchHistoryViewModel : IOpenDocumentViewModel, IDisposable
    {
        public GitBranchHistoryViewModel()
        {
            DocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());
        }
        public MainViewModel? MainViewModel { get; set; }
        public BranchHistoryType Type { get; set; } = BranchHistoryType.Commit;
        public string FilePath { get; set; } = "";


        public DocumentViewModel? Document => null;

        public DocumentId DocumentId { get; private set; }

        public string Title { get; set; }= "Branch Hisotry";

        public bool IsDirty => false;

        public Task AutoSaveAsync()
        {
            return Task.Run(() =>
            {

            });
        }
        public ObservableCollection<CommitItem> Commits { get; } = new ObservableCollection<CommitItem>();
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
