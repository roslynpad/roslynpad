namespace RoslynPad.Themes;

public interface IColorRegistry
{
    string? ResolveDefaultColor(string id, Theme theme);
}
