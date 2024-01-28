using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using RoslynPad.Build;

namespace RoslynPad.UI;

[DebuggerDisplay("{Name}:{IsFolder}")]
public partial class DocumentViewModel : NotificationObject
{
    internal const string AutoSaveSuffix = ".autosave";

    public static ImmutableArray<string> RelevantFileExtensions { get; } = [".cs", ".csx"];

    private bool _isExpanded;
    private bool? _isAutoSaveOnly;
    private bool _isSearchMatch;
    private string? _path;
    private string? _name;
    private string? _orderByName;

    private DocumentViewModel(string rootPath, bool isFolder)
    {
        Path = rootPath;
        IsFolder = isFolder;

        var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(Name);
        IsAutoSave = nameWithoutExtension.EndsWith(AutoSaveSuffix, StringComparison.OrdinalIgnoreCase);
        if (IsAutoSave)
        {
            Name = string.Concat(nameWithoutExtension.AsSpan(0, nameWithoutExtension.Length - AutoSaveSuffix.Length), System.IO.Path.GetExtension(Name));
        }

        IsSearchMatch = true;
    }

    public void ChangePath(string newPath)
    {
        Path = newPath;
        _orderByName = null;
    }

    public string Path
    {
        get => _path.NotNull();
        [MemberNotNull(nameof(_path))]
        private set
        {
            var oldPath = _path;
            if (SetProperty(ref _path, value))
            {
                Name = System.IO.Path.GetFileName(value);
                if (oldPath is not null)
                {
                    UpdateChildPaths(oldPath, value);
                }
            }
        }
    }

    private void UpdateChildPaths(string oldPath, string newPath)
    {
        if (!IsFolder || !IsChildrenInitialized)
        {
            return;
        }

        foreach (var child in InternalChildren)
        {
            child.Path = child.Path.Replace(oldPath, newPath);
        }
    }

    public bool IsFolder { get; }

    public string GetSavePath()
    {
        return IsAutoSave
            ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, Name)
            : Path;
    }

    public string GetAutoSavePath()
    {
        return IsAutoSave ?
            Path
            : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, GetAutoSaveName(Name));
    }

    public static string GetAutoSaveName(string name)
    {
        return System.IO.Path.ChangeExtension(name, AutoSaveSuffix + System.IO.Path.GetExtension(name));
    }

    public static DocumentViewModel CreateRoot(string rootPath)
    {
        IOUtilities.PerformIO(() => Directory.CreateDirectory(rootPath));
        return new DocumentViewModel(rootPath, true);
    }

    public static DocumentViewModel FromPath(string path)
    {
        return new DocumentViewModel(path, isFolder: Directory.Exists(path));
    }

    public DocumentViewModel CreateNew(string documentName)
    {
        if (!IsFolder) throw new InvalidOperationException("Parent must be a folder");

        var document = new DocumentViewModel(GetDocumentPathFromName(Path, documentName), false);
        AddChild(document);
        return document;
    }

    public static string GetDocumentPathFromName(string path, string name) =>
        System.IO.Path.Combine(path, name);

    public void DeleteAutoSave()
    {
        if (IsAutoSave)
        {
            IOUtilities.PerformIO(() => File.Delete(Path));
        }
        else
        {
            var autoSavePath = GetAutoSavePath();
            if (File.Exists(autoSavePath))
            {
                IOUtilities.PerformIO(() => File.Delete(autoSavePath));
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string Name
    {
        get => _name.NotNull();
        private set => SetProperty(ref _name, value);
    }

    public bool IsAutoSave { get; }

    public bool IsAutoSaveOnly =>
        _isAutoSaveOnly ??= IsAutoSave &&
            !File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, Name));

    [MemberNotNullWhen(true, nameof(InternalChildren))]
    public bool IsChildrenInitialized => InternalChildren != null;

    internal DocumentCollection? InternalChildren { get; private set; }

    public ObservableCollection<DocumentViewModel>? Children
    {
        get
        {
            if (IsFolder && InternalChildren == null)
            {
                InternalChildren = ReadChildren();
            }

            return InternalChildren;
        }
    }

    public bool IsSearchMatch
    {
        get => _isSearchMatch;
        internal set => SetProperty(ref _isSearchMatch, value);
    }

    private DocumentCollection ReadChildren()
    {
        var directories =
            IOUtilities.EnumerateDirectories(Path)
            .Select(directory => new DocumentViewModel(directory, isFolder: true))
            .OrderBy(directory => directory.OrderByName);

        var files = Enumerable.Empty<DocumentViewModel>();

        foreach (var extension in RelevantFileExtensions)
        {
            files = files.Concat(IOUtilities.EnumerateFiles(Path, "*" + extension)
                .Select(file => new DocumentViewModel(file, isFolder: false))
                .Where(file => !file.IsAutoSave));
        }

        return new DocumentCollection(directories.Concat(files.OrderBy(file => file.OrderByName)));
    }

    private string OrderByName =>
        _orderByName ??= NumberRegex().Replace(Name, m => m.Value.PadLeft(100, '0'));

    internal void AddChild(DocumentViewModel documentViewModel)
    {
        var children = InternalChildren;
        if (children is null)
        {
            return;
        }

#pragma warning disable CA1309 // Use ordinal string comparison
        var insertIndex = children.IndexOf(d => d.IsFolder == documentViewModel.IsFolder &&
                                                string.Compare(documentViewModel.OrderByName, d.OrderByName,
                                                    StringComparison.CurrentCulture) <= 0);
#pragma warning restore CA1309 // Use ordinal string comparison
        if (insertIndex < 0)
        {
            insertIndex = documentViewModel.IsFolder ? children.IndexOf(c => !c.IsFolder) : children.Count;

            if (insertIndex < 0)
            {
                insertIndex = 0;
            }
        }

        children.Insert(insertIndex, documentViewModel);
    }

    [GeneratedRegex("[0-9]+")]
    private static partial Regex NumberRegex();
}
