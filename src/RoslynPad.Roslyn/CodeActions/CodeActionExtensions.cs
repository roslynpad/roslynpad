using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Glyph = RoslynPad.Roslyn.Completion.Glyph;

namespace RoslynPad.Roslyn.CodeActions;

public static class CodeActionExtensions
{
    public static bool HasCodeActions(this CodeAction codeAction)
    {
        ArgumentNullException.ThrowIfNull(codeAction);

        return !codeAction.NestedCodeActions.IsDefaultOrEmpty;
    }

    public static ImmutableArray<CodeAction> GetCodeActions(this CodeAction codeAction)
    {
        ArgumentNullException.ThrowIfNull(codeAction);

        return codeAction.NestedCodeActions;
    }

    public static Glyph GetGlyph(this CodeAction codeAction)
    {
        ArgumentNullException.ThrowIfNull(codeAction);

        return GetGlyph(codeAction.Tags);
    }

    public static Glyph GetGlyph(ImmutableArray<string> tags)
    {
        foreach (var tag in tags)
        {
            switch (tag)
            {
                case WellKnownTags.Assembly:
                    return Glyph.Assembly;

                case WellKnownTags.File:
                    return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicFile : Glyph.CSharpFile;

                case WellKnownTags.Project:
                    return tags.Contains(LanguageNames.VisualBasic) ? Glyph.BasicProject : Glyph.CSharpProject;

                case WellKnownTags.Class:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.ClassProtected,
                        Accessibility.Private => Glyph.ClassPrivate,
                        Accessibility.Internal => Glyph.ClassInternal,
                        _ => Glyph.ClassPublic,
                    };
                case WellKnownTags.Constant:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.ConstantProtected,
                        Accessibility.Private => Glyph.ConstantPrivate,
                        Accessibility.Internal => Glyph.ConstantInternal,
                        _ => Glyph.ConstantPublic,
                    };
                case WellKnownTags.Delegate:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.DelegateProtected,
                        Accessibility.Private => Glyph.DelegatePrivate,
                        Accessibility.Internal => Glyph.DelegateInternal,
                        _ => Glyph.DelegatePublic,
                    };
                case WellKnownTags.Enum:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.EnumProtected,
                        Accessibility.Private => Glyph.EnumPrivate,
                        Accessibility.Internal => Glyph.EnumInternal,
                        _ => Glyph.EnumPublic,
                    };
                case WellKnownTags.EnumMember:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.EnumMemberProtected,
                        Accessibility.Private => Glyph.EnumMemberPrivate,
                        Accessibility.Internal => Glyph.EnumMemberInternal,
                        _ => Glyph.EnumMemberPublic,
                    };
                case WellKnownTags.Error:
                    return Glyph.Error;

                case WellKnownTags.Event:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.EventProtected,
                        Accessibility.Private => Glyph.EventPrivate,
                        Accessibility.Internal => Glyph.EventInternal,
                        _ => Glyph.EventPublic,
                    };
                case WellKnownTags.ExtensionMethod:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.ExtensionMethodProtected,
                        Accessibility.Private => Glyph.ExtensionMethodPrivate,
                        Accessibility.Internal => Glyph.ExtensionMethodInternal,
                        _ => Glyph.ExtensionMethodPublic,
                    };
                case WellKnownTags.Field:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.FieldProtected,
                        Accessibility.Private => Glyph.FieldPrivate,
                        Accessibility.Internal => Glyph.FieldInternal,
                        _ => Glyph.FieldPublic,
                    };
                case WellKnownTags.Interface:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.InterfaceProtected,
                        Accessibility.Private => Glyph.InterfacePrivate,
                        Accessibility.Internal => Glyph.InterfaceInternal,
                        _ => Glyph.InterfacePublic,
                    };
                case WellKnownTags.Intrinsic:
                    return Glyph.Intrinsic;

                case WellKnownTags.Keyword:
                    return Glyph.Keyword;

                case WellKnownTags.Label:
                    return Glyph.Label;

                case WellKnownTags.Local:
                    return Glyph.Local;

                case WellKnownTags.Namespace:
                    return Glyph.Namespace;

                case WellKnownTags.Method:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.MethodProtected,
                        Accessibility.Private => Glyph.MethodPrivate,
                        Accessibility.Internal => Glyph.MethodInternal,
                        _ => Glyph.MethodPublic,
                    };
                case WellKnownTags.Module:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.ModulePublic,
                        Accessibility.Private => Glyph.ModulePrivate,
                        Accessibility.Internal => Glyph.ModuleInternal,
                        _ => Glyph.ModulePublic,
                    };
                case WellKnownTags.Folder:
                    return Glyph.OpenFolder;

                case WellKnownTags.Operator:
                    return Glyph.Operator;

                case WellKnownTags.Parameter:
                    return Glyph.Parameter;

                case WellKnownTags.Property:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.PropertyProtected,
                        Accessibility.Private => Glyph.PropertyPrivate,
                        Accessibility.Internal => Glyph.PropertyInternal,
                        _ => Glyph.PropertyPublic,
                    };
                case WellKnownTags.RangeVariable:
                    return Glyph.RangeVariable;

                case WellKnownTags.Reference:
                    return Glyph.Reference;

                case WellKnownTags.NuGet:
                    return Glyph.NuGet;

                case WellKnownTags.Structure:
                    return GetAccessibility(tags) switch
                    {
                        Accessibility.Protected => Glyph.StructureProtected,
                        Accessibility.Private => Glyph.StructurePrivate,
                        Accessibility.Internal => Glyph.StructureInternal,
                        _ => Glyph.StructurePublic,
                    };
                case WellKnownTags.TypeParameter:
                    return Glyph.TypeParameter;

                case WellKnownTags.Snippet:
                    return Glyph.Snippet;

                case WellKnownTags.Warning:
                    return Glyph.CompletionWarning;

                case WellKnownTags.StatusInformation:
                    return Glyph.StatusInformation;
            }
        }

        return Glyph.None;
    }

    private static Accessibility GetAccessibility(ImmutableArray<string> tags)
    {
        if (tags.Contains(WellKnownTags.Public))
        {
            return Accessibility.Public;
        }
        if (tags.Contains(WellKnownTags.Protected))
        {
            return Accessibility.Protected;
        }
        if (tags.Contains(WellKnownTags.Internal))
        {
            return Accessibility.Internal;
        }
        if (tags.Contains(WellKnownTags.Private))
        {
            return Accessibility.Private;
        }
        return Accessibility.NotApplicable;
    }
}
