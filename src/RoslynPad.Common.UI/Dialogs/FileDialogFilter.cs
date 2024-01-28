namespace RoslynPad.UI;

public record FileDialogFilter(string Header, IList<string> Extensions)
{
    public FileDialogFilter(string header, params string[] extensions)
        : this(header, (IList<string>)extensions)
    {
    }

    public override string ToString() => 
        $"{Header}|{string.Join(";", Extensions.Select(e => "*." + e))}";
}
