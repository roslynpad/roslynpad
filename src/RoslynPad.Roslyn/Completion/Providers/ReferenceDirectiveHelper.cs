internal static class ReferenceDirectiveHelper
{
    public const string NuGetPrefix = "nuget:";

    private static readonly char[] s_nugetSeparators = new[] { '/', ',' };

    public static (string id, string? version) ParseNuGetReference(string value)
    {
        string id;
        string? version;

        var indexOfSlash = value.IndexOfAny(s_nugetSeparators);
        if (indexOfSlash >= 0)
        {
            id = value.Substring(NuGetPrefix.Length, indexOfSlash - NuGetPrefix.Length);
            version = indexOfSlash != value.Length - 1 ? value.Substring(indexOfSlash + 1) : string.Empty;
        }
        else
        {
            id = value.Substring(NuGetPrefix.Length);
            version = null;
        }

        return (id.Trim(), version?.Trim());
    }
}
