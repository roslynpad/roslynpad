namespace RoslynPad.Roslyn.Classification;

[Flags]
public enum TaggedTextStyle
{
    None = 0,
    Strong = 1 << 0,
    Emphasis = 1 << 1,
    Underline = 1 << 2,
    Code = 1 << 3,
    PreserveWhitespace = 1 << 4,
}
