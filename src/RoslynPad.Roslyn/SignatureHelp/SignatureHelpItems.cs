using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.SignatureHelp;

public class SignatureHelpItems
{
    public IList<SignatureHelpItem> Items { get; }

    public TextSpan ApplicableSpan { get; }

	public int SemanticParameterIndex { get; }

	public int SyntacticArgumentCount { get; }

    public string? ArgumentName { get; }

    public int? SelectedItemIndex { get; internal set; }

    internal SignatureHelpItems(Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpItems inner)
    {
        Items = inner.Items.Select(x => new SignatureHelpItem(x)).ToArray();
        ApplicableSpan = inner.ApplicableSpan;
        SemanticParameterIndex = inner.SemanticParameterIndex;
        SyntacticArgumentCount = inner.SyntacticArgumentCount;
        ArgumentName = inner.ArgumentName;
        SelectedItemIndex = inner.SelectedItemIndex;
    }
}
