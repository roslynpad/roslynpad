using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NuGet.Versioning;

namespace RoslynPad.Roslyn.FileBasedPrograms;

/// <summary>
/// Parsing for the legacy <c>#r</c> reference directives, and migration of those directives to
/// their file-based app equivalents (<c>#:package</c>, <c>#:sdk</c>). <c>#r</c> is only legal in
/// scripts, so a regular C# file carrying one reports a syntax error until it is migrated.
/// </summary>
public static class ReferenceDirectiveHelpers
{
    public const string NuGetPrefix = "nuget:";
    public const string LegacyNuGetPrefix = @"$NuGet\";
    public const string FrameworkPrefix = "framework:";

    private static readonly SearchValues<char> s_nugetSeparators = SearchValues.Create('/', ',');

    /// <summary>
    /// Shared frameworks reachable from a file-based app, which has no <c>FrameworkReference</c>
    /// directive of its own and pulls them in through its SDK instead.
    /// </summary>
    private static readonly FrozenDictionary<string, string> s_frameworkSdks = FrozenDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase,
        new("Microsoft.AspNetCore.App", "Microsoft.NET.Sdk.Web"),
        new("Microsoft.WindowsDesktop.App", "Microsoft.NET.Sdk.WindowsDesktop")
    );

    /// <summary>
    /// Returns the edits that rewrite every migratable <c>#r</c> directive preceding the file's
    /// code into a file-based app directive, or an empty array when there is nothing to migrate.
    /// Directives with no equivalent - plain assembly paths, unrecognized frameworks - are left
    /// alone, as are directives placed after code, which <c>#:</c> syntax may not follow.
    /// </summary>
    public static ImmutableArray<TextChange> GetMigrationChanges(SyntaxNode root, SourceText text)
    {
        var builder = ImmutableArray.CreateBuilder<TextChange>();

        foreach (var trivia in root.GetLeadingTrivia())
        {
            if (trivia.GetStructure() is ReferenceDirectiveTriviaSyntax directive &&
                Migrate(directive.File.ValueText) is { } replacement)
            {
                var line = text.Lines.GetLineFromPosition(trivia.SpanStart);
                builder.Add(new TextChange(TextSpan.FromBounds(line.Start, line.End), replacement));
            }
        }

        return builder.ToImmutable();
    }

    private static string? Migrate(string value)
    {
        if (HasPrefix(FrameworkPrefix, value))
        {
            return s_frameworkSdks.TryGetValue(value[FrameworkPrefix.Length..].Trim(), out var sdk)
                ? $"#:sdk {sdk}"
                : null;
        }

        var (id, version) =
            HasPrefix(NuGetPrefix, value) ? ParseNuGetReference(value) :
            HasPrefix(LegacyNuGetPrefix, value) ? ParseLegacyNuGetReference(value) :
            default;

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return string.IsNullOrEmpty(version) ? $"#:package {id}@*"
            : VersionRange.TryParse(version, out _) ? $"#:package {id}@{version}"
            : null;
    }

    public static bool HasPrefix(string prefix, string value) =>
        value.Length > prefix.Length &&
        value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);

    /// <summary>Parses <c>nuget: Name, Version</c> or <c>nuget: Name/Version</c>.</summary>
    public static (string id, string? version) ParseNuGetReference(string value)
    {
        string id;
        string? version;

        var indexOfSlash = value.AsSpan().IndexOfAny(s_nugetSeparators);
        if (indexOfSlash >= 0)
        {
            id = value[NuGetPrefix.Length..indexOfSlash];
            version = indexOfSlash != value.Length - 1 ? value[(indexOfSlash + 1)..] : string.Empty;
        }
        else
        {
            id = value[NuGetPrefix.Length..];
            version = null;
        }

        return (id.Trim(), version?.Trim());
    }

    /// <summary>Parses <c>$NuGet\Name\Version\...</c>.</summary>
    public static (string? id, string? version) ParseLegacyNuGetReference(string value)
    {
        var split = value.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return split.Length >= 3 ? (split[1], split[2]) : (null, null);
    }

    /// <summary>Parses the value of a <c>#:property</c> directive: <c>Name=Value</c>.</summary>
    public static (string name, string? value) ParsePropertyDirective(string directiveText)
    {
        var equalsIndex = directiveText.IndexOf('=');
        if (equalsIndex < 0)
        {
            return (directiveText.Trim(), null);
        }

        return (directiveText[..equalsIndex].Trim(), directiveText[(equalsIndex + 1)..].Trim());
    }

    /// <summary>Parses the value of a <c>#:package</c> directive: <c>Name@Version</c> or <c>Name</c>.</summary>
    public static (string id, string? version) ParsePackageDirective(string directiveText)
    {
        var atIndex = directiveText.IndexOf('@');
        if (atIndex < 0)
        {
            return (directiveText.Trim(), null);
        }

        var version = directiveText[(atIndex + 1)..].Trim();
        return (directiveText[..atIndex].Trim(), string.IsNullOrEmpty(version) ? null : version);
    }
}
