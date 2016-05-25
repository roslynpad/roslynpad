namespace RoslynPad.Roslyn.Snippets
{
    public sealed class SnippetInfo
    {
        public string Shortcut { get; }

        public string Title { get; }

        public string Description { get; }

        public SnippetInfo(string shortcut, string title, string description)
        {
            Shortcut = shortcut;
            Title = title;
            Description = description;
        }
    }
}