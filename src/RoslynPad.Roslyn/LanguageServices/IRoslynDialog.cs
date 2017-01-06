namespace RoslynPad.Roslyn.LanguageServices
{
    public interface IRoslynDialog
    {
        object ViewModel { get; set; }

        bool? Show();
    }
}