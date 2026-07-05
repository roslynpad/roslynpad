namespace Morgania.CodeAnalysis.Editor.LanguageServices;

internal interface IRoslynDialog
{
    object ViewModel { get; set; }

    bool? Show();
}
