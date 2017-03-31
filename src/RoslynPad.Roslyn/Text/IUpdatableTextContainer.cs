using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Text
{
    public interface IUpdatableTextContainer
    {
        void UpdateText(SourceText text);
    }
}