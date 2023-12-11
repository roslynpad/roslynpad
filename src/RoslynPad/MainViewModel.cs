using System;
using System.Composition;
using System.Reflection;
using RoslynPad.UI;
using System.Collections.Immutable;
using RoslynPad.Utilities;
using LibGit2Sharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RoslynPad;

[Export(typeof(MainViewModelBase)), Shared]
public class MainViewModel : MainViewModelBase
{
    protected override ImmutableArray<Assembly> CompositionAssemblies => base.CompositionAssemblies
        .Add(Assembly.Load(new AssemblyName("RoslynPad.Roslyn.Windows")))
        .Add(Assembly.Load(new AssemblyName("RoslynPad.Editor.Windows")));


    bool gitInited;

    public bool IsGitInited
    {
        get { return gitInited; }
        set { SetProperty<bool>(ref gitInited, value); }
    }
    public IDelegateCommand InitGitCommand { get; }
    public IDelegateCommand CommitCommand { get; }
    public IDelegateCommand BrachHistoryCommand { get; }
    Repository? repository;
    string repositroyPath;
    [ImportingConstructor]
    public MainViewModel(IServiceProvider serviceProvider, ITelemetryProvider telemetryProvider, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel, DocumentFileWatcher documentFileWatcher) : base(serviceProvider, telemetryProvider, commands, settings, nugetViewModel, documentFileWatcher)
    {
        InitGitCommand = commands.CreateAsync(InitGit);
        CommitCommand = commands.Create(Commit);
        BrachHistoryCommand = commands.Create(ViewBranchHistory);
        repositroyPath = "";
        CheckGit();
    }
    /// <summary>
    /// open document from path, if document if not csx, open it with system editor
    /// </summary>
    /// <param name="path"></param>
    public void OpenDocument(string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(DocumentRoot.Path, path);
        foreach (var d in OpenDocuments)
        {
            if (d.Document != null && d.Document.Path == path)
            {
                CurrentOpenDocument = d;
                OnPropertyChanged(nameof(CurrentOpenDocument));
                return;
            }
        }
        if (path.EndsWith(".csx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var document = DocumentViewModel.FromPath(path);
            OpenDocument(document);
        }
        else
        {
            Process.Start(path);
        }
    }
    public override void EditUserDocumentPath()
    {
        base.EditUserDocumentPath();
        CheckGit();
    }
    void CheckGit()
    {
        IsGitInited = false;
        if (repository != null)
        {
            repository.Dispose();
            repository = null;
        }
        try
        {
            var path = DocumentRoot.Path;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                var name = Repository.Discover(path);
                if (!string.IsNullOrEmpty(name))
                {
                    repository = new Repository(name);
                    repositroyPath = GetGitPath(name);
                    LoadIgnore();
                    AddIgnore();
                }
            }
        }
        catch { }
        IsGitInited = repository != null;
    }
    string GetGitPath(string name)
    {
        if (name.EndsWith(".git\\", StringComparison.OrdinalIgnoreCase))
        {
            var parent = new DirectoryInfo(name).Parent;
            if (parent == null) return name;
            return parent.FullName;
        }
        return name;
    }
    public async Task InitGit()
    {
        await Task.Run(() =>
        {
            var path = DocumentRoot.Path;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                var name = Repository.Discover(path);
                if (string.IsNullOrEmpty(name))
                {
                    //copy .ignore and load .ignore
                    var ignorePath = Path.Combine(DocumentRoot.Path, ".gitignore");
                    if (!File.Exists(ignorePath))
                    {
                        using (var stream = this.GetType().Assembly.GetManifestResourceStream("RoslynPad.Resources..gitignore"))
                        {
                            if (stream != null)
                            {
                                var bytes = new byte[2048];
                                int read;
                                using (var writer = new FileStream(ignorePath, FileMode.Create))
                                {
                                    while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
                                        writer.Write(bytes, 0, read);
                                }
                            }
                        }
                    }
                    Repository.Init(path);
                    repository = new Repository(path);
                    repositroyPath = GetGitPath(path);
                    var user = new Signature(Environment.UserName, Environment.UserName + "@annymous", DateTimeOffset.Now);
                    LoadIgnore();
                    AddIgnore();
                    Commands.Stage(repository, ".gitignore");
                    repository.Commit("add .gitignore", user, user);
                }
            }
        }).ConfigureAwait(true);
        IsGitInited = true;
    }
    List<string> ignores = new List<string>()
        {"*.autosave.csx", "*.autosave.cs", ".vs", "RoslynPad.json", "RoslynPad.nuget.config"
        };
    void LoadIgnore()
    {
        var path = Path.Combine(DocumentRoot.Path, ".gitignore");
        if (File.Exists(path))
        {
            ignores.Clear();
            using (StreamReader reader = new StreamReader(path))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith('#')) continue;
                    ignores.Add(line);
                }
            }
        }
    }
    void AddIgnore()
    {
        if (repository == null) return;
        repository.Ignore.AddTemporaryRules(ignores);
    }
    public void Commit()
    {
        if (repository != null)
        {
            var changes = GetChanges(repository);
            if (changes.Children.Count == 0)
            {
                Console.WriteLine("no changes");
                return;
            }
            changes.MainViewModel = this;
            OpenDocuments.Add(changes);
            CurrentOpenDocument = changes;
        }
    }
    public void ViewBranchHistory()
    {
        if (repository != null)
        {
            GitBranchHistoryViewModel vm = new GitBranchHistoryViewModel();
            foreach (var i in repository.Commits)
            {
                vm.Commits.Add(new CommitItem(i.Id + "", i.Committer.When.DateTime, i.Committer.Name, i.MessageShort));
            }
            if (vm.Commits.Count == 0) return;
            vm.MainViewModel = this;
            OpenDocuments.Add(vm);
            CurrentOpenDocument = vm;
        }
    }
    public void ShowFileHistory(string path)
    {
        if (repository == null) return;
        path = PathExtension.RelativePath(repositroyPath, path);
        GitBranchHistoryViewModel vm = new GitBranchHistoryViewModel();
        vm.Type = BranchHistoryType.File;
        vm.FilePath = path;
        foreach (var i in repository.Commits)
        {
            Tree commitTree = i.Tree;
            var parentCommit = i.Parents.FirstOrDefault();
            if (parentCommit == null)
            {
                var record = i.Tree[path];
                if (record != null)
                    vm.Commits.Add(new CommitItem(i.Id + "", i.Committer.When.DateTime, i.Committer.Name, i.MessageShort));
            }
            else
            {
                Tree parentCommitTree = parentCommit.Tree;
                var changes = repository.Diff.Compare<TreeChanges>(parentCommitTree, commitTree);
                foreach (var c in changes)
                {
                    if (c.Status != ChangeKind.Deleted && c.Path == path)
                        vm.Commits.Add(new CommitItem(i.Id + "", i.Committer.When.DateTime, i.Committer.Name, i.MessageShort));
                }
            }
        }
        if (vm.Commits.Count == 0) return;
        vm.MainViewModel = this;
        vm.Title = path + " - Hisotry";
        OpenDocuments.Add(vm);
        CurrentOpenDocument = vm;
    }
    /// <summary>
    /// view detail of commit
    /// </summary>
    /// <param name="id"></param>
    public void ShowCommitDetail(string id)
    {
        if (repository == null) return;
        var commit = repository.Lookup<Commit>(id);
        if (commit == null) return;
        var changes = GetChanges(repository, commit);
        changes.Title = string.Concat( "Commit detail - ", (commit.Id + "").Substring(0, 8));
        changes.MainViewModel = this;
        OpenDocuments.Add(changes);
        CurrentOpenDocument = changes;
    }
    /// <summary>
    /// view file in commit
    /// </summary>
    /// <param name="id"></param>
    /// <param name="path"></param>
    public void ShowCommitFile(string id, string path)
    {
        if (repository == null) return;
        var commit = repository.Lookup<Commit>(id);
        if (commit == null) return;
        var item = commit.Tree[path];
        if (item == null) return;
        //(item.Target as Blob).
        var blob = item.Target as Blob;
        if (blob == null) return;
        GitCommitFileViewModel vm = new GitCommitFileViewModel(path, blob, this);
        vm.Title = string.Concat( path, "-" , id.Substring(0, 8));
        OpenDocuments.Add(vm);
        CurrentOpenDocument = vm;
    }
    public void CompareFileWithCurrent(string commitId, string path)
    {
        if (repository == null) return;
        var vm = GitFileCompareViewModel.CompareFile(repository, commitId, path);
        if (vm == null) return;
        vm.Title = "Diff - " + path;
        vm.MainViewModel = this;
        OpenDocuments.Add(vm);
        CurrentOpenDocument = vm;
    }
    public void CompareFileWithPreviouse(string commitId, string path)
    {
        if (repository == null) return;
        List<string> commitIds = new List<string>();
        foreach (var i in repository.Commits)
        {
            Tree commitTree = i.Tree;
            var parentCommit = i.Parents.FirstOrDefault();
            if (parentCommit == null)
            {
                var record = i.Tree[path];
                if (record != null)
                    commitIds.Add(i.Id + "");
            }
            else
            {
                Tree parentCommitTree = parentCommit.Tree;
                var changes = repository.Diff.Compare<TreeChanges>(parentCommitTree, commitTree);
                foreach (var c in changes)
                {
                    if (c.Status != ChangeKind.Deleted && c.Path == path)
                        commitIds.Add(i.Id + "");
                }
            }
        }
        int index = commitIds.IndexOf(commitId);
        if (index < 0) return;
        index++;
        if (index >= commitIds.Count) return;
        string previouseCommit = commitIds[index];
        CompareFileWithCommits(commitId, path, previouseCommit);
    }
    public void CompareFileWithCommits(string commitId, string path, string previousCommitId)
    {
        if (repository == null) return;
        var vm = GitFileCompareViewModel.CompareFile(repository, commitId, path, previousCommitId);
        if (vm == null) return;
        vm.Title = "Diff - " + path;
        vm.MainViewModel = this;
        OpenDocuments.Add(vm);
        CurrentOpenDocument = vm;
    }
    public void CompareFile(string path)
    {
        if (repository == null) return;
        var vm = GitFileCompareViewModel.CompareFile(repository, path);
        if (vm == null) return;
        vm.Title = "Diff - " + path;
        vm.MainViewModel = this;
        OpenDocuments.Add(vm);
        CurrentOpenDocument = vm;
    }
    internal async void GitCommit(GitChangesViewModel document, string comment)
    {
        if (repository == null) return;
        Commands.Stage(repository, "*");
        var user = new Signature(Environment.UserName, Environment.UserName + "@annymous", DateTimeOffset.Now);
        repository.Commit(comment, user, user);
        await CloseDocument(document).ConfigureAwait(true);
    }
    internal async void GitIgnore(GitChangesViewModel document, string path)
    {
        if (repository == null) return;
        ignores.Add(path);
        var ignorepath = Path.Combine(DocumentRoot.Path, ".gitignore");
        if (File.Exists(ignorepath))
        {
            ignores.Clear();
            using (StreamWriter writer = new StreamWriter(ignorepath, true))
            {
                writer.WriteLine(path);
            }
        }
        AddIgnore();
        var changes = GetChanges(repository);
        document.Children.Clear();
        foreach (var i in changes.Children)
            document.Children.Add(i);
        if (document.Children.Count == 0) await CloseDocument(document).ConfigureAwait(true);
    }
    private GitChangesViewModel GetChanges(Repository repo)
    {
        var models = new GitChangesViewModel("", "");
        models.MainViewModel = this;
        models.Title = "Commit Changes";
        var changes = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory);
        foreach (var i in changes)
        {
            string? path = Path.GetDirectoryName(i.Path);
            if (string.IsNullOrEmpty(path))
                models.Children.Add(new GitChangesViewModel(i.Path, (i.Status + "").Substring(0, 1)));
            else
            {
                var pathModel = models;
                var paths = path.Split('\\', '/');
                foreach (var p in paths)
                {
                    GitChangesViewModel? child = null;
                    foreach (var c in pathModel.Children)
                    {
                        if (c.IsFolder && c.Name == p)
                            child = c;
                    }
                    if (child == null)
                    {
                        child = new GitChangesViewModel(p, "", true);
                        pathModel.Children.Insert(0, child);
                    }
                    pathModel = child;
                }
                pathModel.Children.Add(new GitChangesViewModel(i.Path, (i.Status + "").Substring(0, 1)));
            }
        }
        return models;
    }
    CommitChangesViewModel GetChanges(Tree tree, string path)
    {
        CommitChangesViewModel models = new CommitChangesViewModel(path, "A");
        foreach (var i in tree)
        {
            if (i.TargetType != TreeEntryTargetType.Tree)
            {
                models.Children.Add(new CommitChangesViewModel(i.Path, "A"));
            }
            else if (i.Target is Tree t)
            {
                models.Children.Add(GetChanges(t, i.Path));
            }
        }
        return models;
    }
    private CommitChangesViewModel GetChanges(Repository repo, Commit commit)
    {
        var models = new CommitChangesViewModel("", "");
        models.CommitId = commit.Id + "";
        Tree commitTree = commit.Tree;
        var parentCommit = commit.Parents.FirstOrDefault();
        if (parentCommit == null)
        {
            models = GetChanges(commitTree, "");
            models.CommitId = commit.Id + "";
            return models;
        }
        Tree parentCommitTree = parentCommit.Tree;
        var changes = repo.Diff.Compare<TreeChanges>(parentCommitTree, commitTree);

        foreach (TreeEntryChanges i in changes)
        {
            string? path = Path.GetDirectoryName(i.Path);
            if (string.IsNullOrEmpty(path))
                models.Children.Add(new CommitChangesViewModel(i.Path, (i.Status + "").Substring(0, 1)));
            else
            {
                var pathModel = models;
                var paths = path.Split('\\', '/');
                foreach (var p in paths)
                {
                    CommitChangesViewModel? child = null;
                    foreach (var c in pathModel.Children)
                    {
                        if (c.IsFolder && c.Name == p)
                            child = c;
                    }
                    if (child == null)
                    {
                        child = new CommitChangesViewModel(p, "", true);
                        pathModel.Children.Insert(0, child);
                    }
                    pathModel = child;
                }
                pathModel.Children.Add(new CommitChangesViewModel(i.Path, (i.Status + "").Substring(0, 1)));
            }
        }
        return models;
    }

}
