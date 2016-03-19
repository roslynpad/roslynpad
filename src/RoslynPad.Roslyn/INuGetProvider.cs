namespace RoslynPad.Roslyn
{
    public interface INuGetProvider
    {
        string PathToRepository { get; }

        string PathVariableName { get; }
    }
}