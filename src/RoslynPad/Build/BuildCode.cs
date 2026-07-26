using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynPad.Build;

internal static class BuildCode
{
    public const string ScriptInit = "RoslynPad.Runtime.RuntimeInitializer.Initialize();";

    public const string ModuleInitAttributeFileName = "ModuleInitializerAttribute.cs";

    public const string ModuleInitAttribute = @"
            using System;

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public sealed class ModuleInitializerAttribute : Attribute { }
            }
        ";

    public const string ModuleInitFileName = "ModuleInitializer.cs";

    public const string ModuleInit = @"
            internal static class ModuleInitializer
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void Initialize() =>
                    RoslynPad.Runtime.RuntimeInitializer.Initialize();
            }
        ";

    /// <summary>
    /// Finds the last bare expression (a global expression statement with a missing semicolon)
    /// that execution rewrites into a Dump call, so its CS1002/CA1806 diagnostics can be hidden.
    /// </summary>
    public static GlobalStatementSyntax? FindTrailingExpression(CompilationUnitSyntax compilationUnit) =>
        compilationUnit.Members.OfType<GlobalStatementSyntax>()
            .LastOrDefault(m => m.Statement is ExpressionStatementSyntax expr && expr.SemicolonToken.IsMissing);

    // The receiver is parenthesized because the rewritten tree round-trips through text
    // (written to Program.cs/.csx and re-parsed by the build) — without parentheses a binary
    // trailing expression like "1 + 2" would re-parse as "1 + 2.Dump()".
    public static GlobalStatementSyntax GetDumpCall(ExpressionStatementSyntax statement) =>
        GlobalStatement(
            ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParenthesizedExpression(statement.Expression),
                    IdentifierName("Dump")))));
}
