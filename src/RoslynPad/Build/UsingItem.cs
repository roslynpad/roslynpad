namespace RoslynPad.Build;

internal sealed record UsingItem(string Identity, bool Static, string? Alias)
{
    public static UsingItem Create(string identity) => new(identity, Static: false, Alias: null);

    public string? CompilationOption => Static || !string.IsNullOrEmpty(Alias) ? null : Identity;

    public string GlobalUsingDirective =>
        Static ? $"global using static {Identity};" :
        !string.IsNullOrEmpty(Alias) ? $"global using {Alias} = {Identity};" :
        $"global using {Identity};";
}
