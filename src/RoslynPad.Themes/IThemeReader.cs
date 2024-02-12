
namespace RoslynPad.Themes
{
    public interface IThemeReader
    {
        Task<Theme> ReadThemeAsync(string file, ThemeType type);
    }
}
