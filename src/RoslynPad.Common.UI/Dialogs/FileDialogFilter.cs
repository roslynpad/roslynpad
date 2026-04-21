namespace RoslynPad.UI;

public record FileDialogFilter(string Header, params IReadOnlyList<string> Extensions);