internal static class ReferenceDirectiveHelper
{
    public const string NuGetPrefix = "nuget:";

    public const string FileBasedPackagePrefix = "#:package ";
    public const string FileBasedFrameworkPrefix = "#:framework ";

    private static readonly char[] s_nugetSeparators = ['/', ','];

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

    /// <summary>
    /// Parses a file-based package reference: #:package Name@Version or #:package Name
    /// </summary>
    public static (string id, string? version) ParseFileBasedPackageReference(string value)
    {
        // value is the full line starting with #:package
        var packagePart = value.Substring(FileBasedPackagePrefix.Length).Trim();

        var atIndex = packagePart.IndexOf('@');
        if (atIndex >= 0)
        {
            var id = packagePart.Substring(0, atIndex).Trim();
            var version = packagePart.Substring(atIndex + 1).Trim();
            return (id, string.IsNullOrEmpty(version) ? null : version);
        }

        return (packagePart, null);
    }

    /// <summary>
    /// Parses the directive text of a file-based package reference: Name@Version or Name
    /// </summary>
    public static (string id, string? version) ParseFileBasedPackageDirective(string directiveText)
    {
        // directiveText is just the value part (e.g., "System.CommandLine@2.0.0-*")
        var atIndex = directiveText.IndexOf('@');
        if (atIndex >= 0)
        {
            var id = directiveText.Substring(0, atIndex).Trim();
            var version = directiveText.Substring(atIndex + 1).Trim();
            return (id, string.IsNullOrEmpty(version) ? null : version);
        }

        return (directiveText.Trim(), null);
    }
}
