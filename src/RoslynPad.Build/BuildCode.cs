using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynPad.Build
{
    internal static class BuildCode
    {
        public const string ScriptInit = "RoslynPad.Runtime.RuntimeInitializer.Initialize();";

        public const string ModuleInitAttribute = @"
            using System;

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
                public sealed class ModuleInitializerAttribute : Attribute { }
            }
        ";

        public const string ModuleInit = @"
            internal static class ModuleInitializer
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void Initialize() =>
                    RoslynPad.Runtime.RuntimeInitializer.Initialize();
            }
        ";

        public static GlobalStatementSyntax GetDumpCall(ExpressionStatementSyntax statement) =>
            GlobalStatement(
                ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        statement.Expression,
                        IdentifierName("Dump")))));
    }
}
