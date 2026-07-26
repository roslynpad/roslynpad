namespace RoslynPad.Build;

/// <summary>The build phase a streamed output line belongs to.</summary>
internal enum BuildOutputSource
{
    Restore,
    Compile,
}
