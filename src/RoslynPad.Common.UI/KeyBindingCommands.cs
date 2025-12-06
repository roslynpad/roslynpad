using System.Runtime.InteropServices;

namespace RoslynPad.UI;

/// <summary>
/// Defines all known commands with their identifiers, descriptions, and default key bindings.
/// Uses VS Code-style command identifiers for familiarity.
/// </summary>
public static class KeyBindingCommands
{
    // Editor commands (from VS Code)
    public const string RenameSymbol = "editor.action.rename";
    public const string CommentSelection = "editor.action.commentLine";
    public const string UncommentSelection = "editor.action.removeCommentLine";
    public const string FormatDocument = "editor.action.formatDocument";

    // File commands (from VS Code)
    public const string SaveDocument = "workbench.action.files.save";
    public const string OpenFile = "workbench.action.files.openFile";
    public const string CloseCurrentFile = "workbench.action.closeActiveEditor";

    // Debug commands (from VS Code)
    public const string RunScript = "workbench.action.debug.start";
    public const string TerminateRunningScript = "workbench.action.debug.stop";

    // RoslynPad-specific commands
    public const string NewDocument = "roslynpad.newDocument";
    public const string NewScript = "roslynpad.newScript";
    public const string ToggleOptimization = "roslynpad.toggleOptimization";
    public const string ResultsCopyValue = "roslynpad.results.copy";
    public const string ResultsCopyValueWithChildren = "roslynpad.results.copyWithChildren";
    public const string SearchReplaceNext = "roslynpad.search.replaceNext";
    public const string SearchReplaceAll = "roslynpad.search.replaceAll";

    /// <summary>
    /// Metadata for a command including its description and default key bindings.
    /// </summary>
    public sealed record CommandInfo(string Command, string Description, string WindowsKey, string MacKey)
    {
        public string GetDefaultKey() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? MacKey : WindowsKey;
    }

    /// <summary>
    /// All known commands with their metadata.
    /// </summary>
    public static IReadOnlyDictionary<string, CommandInfo> All { get; } = new Dictionary<string, CommandInfo>
    {
        [RenameSymbol] = new(RenameSymbol, "Rename Symbol", "F2", "F2"),
        [CommentSelection] = new(CommentSelection, "Comment Selection", "Ctrl+K", "Cmd+K"),
        [UncommentSelection] = new(UncommentSelection, "Uncomment Selection", "Ctrl+U", "Cmd+U"),
        [FormatDocument] = new(FormatDocument, "Format Document", "Ctrl+D", "Cmd+D"),
        [SaveDocument] = new(SaveDocument, "Save Document", "Ctrl+S", "Cmd+S"),
        [OpenFile] = new(OpenFile, "Open File", "Ctrl+O", "Cmd+O"),
        [CloseCurrentFile] = new(CloseCurrentFile, "Close File", "Ctrl+W", "Cmd+W"),
        [RunScript] = new(RunScript, "Run Script", "F5", "F5"),
        [TerminateRunningScript] = new(TerminateRunningScript, "Terminate Script", "Shift+F5", "Shift+F5"),
        [NewDocument] = new(NewDocument, "New Document", "Ctrl+N", "Cmd+N"),
        [NewScript] = new(NewScript, "New Script", "Ctrl+Shift+N", "Cmd+Shift+N"),
        [ToggleOptimization] = new(ToggleOptimization, "Toggle Optimization", "Ctrl+Shift+O", "Cmd+Shift+O"),
        [ResultsCopyValue] = new(ResultsCopyValue, "Copy Value", "Ctrl+C", "Cmd+C"),
        [ResultsCopyValueWithChildren] = new(ResultsCopyValueWithChildren, "Copy Value with Children", "Ctrl+Shift+C", "Cmd+Shift+C"),
        [SearchReplaceNext] = new(SearchReplaceNext, "Replace Next", "Alt+R", "Alt+R"),
        [SearchReplaceAll] = new(SearchReplaceAll, "Replace All", "Alt+A", "Alt+A"),
    };
}
