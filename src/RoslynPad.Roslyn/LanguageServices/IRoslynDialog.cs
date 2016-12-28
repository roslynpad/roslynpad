namespace RoslynPad.Roslyn.LanguageServices
{
    internal interface IRoslynDialog
    {
        object ViewModel { get; set; }

        bool? Show();
    }
}