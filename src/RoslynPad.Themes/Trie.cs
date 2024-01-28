namespace RoslynPad.Themes;

internal class Trie<T>
{
    private const char Separator = '.';
    private readonly TrieNode _root = new();

    public void TryAdd(string key, T value)
    {
        var node = _root;
        var parts = key.Split(Separator);
        foreach (var part in parts)
        {
            if (!node.Children.TryGetValue(part, out var childNode))
            {
                childNode = new TrieNode();
                node.Children.Add(part, childNode);
            }

            node = childNode;
        }

        node.Value ??= new KeyValuePair<string, T>(key, value);
    }

    public KeyValuePair<string, T>? FindLongestPrefix(string key)
    {
        var node = _root;
        var parts = key.Split(Separator);
        foreach (var part in parts)
        {
            if (!node.Children.TryGetValue(part, out var childNode))
            {
                break;
            }

            node = childNode;
        }

        return node.Value;
    }

    private class TrieNode
    {
        public Dictionary<string, TrieNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public KeyValuePair<string, T>? Value { get; set; }
    }
}
