using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Castle.Core.Interceptor;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn
{
    internal sealed class RoslynInterfaceProxy : InterfaceProxyWithoutTargetGenerator
    {
        internal const string WorkspaceField = "_workspace";
        private const string InitializeMethod = "Initialize";

        private static readonly ModuleScope ModuleScope = new ModuleScope();

        private RoslynInterfaceProxy(Type @interface) : base(ModuleScope, @interface)
        {
        }

        protected override Type Init(string typeName, out ClassEmitter emitter, Type proxyTargetType, out FieldReference interceptorsField, IEnumerable<Type> interfaces)
        {
            var type = base.Init(typeName, out emitter, proxyTargetType, out interceptorsField, interfaces);

            emitter.CreateDefaultConstructor();

            var workspaceField = emitter.CreateField(WorkspaceField, typeof(Workspace));

            var method = emitter.CreateMethod(InitializeMethod, MethodAttributes.Public);
            method.SetReturnType(typeof(void));
            method.SetParameters(new[] { typeof(IInterceptor[]), typeof(Workspace) });
            method.CodeBuilder.AddStatement(new AssignStatement(interceptorsField, new ReferenceExpression(method.Arguments[0])));
            method.CodeBuilder.AddStatement(new AssignStatement(workspaceField, new ReferenceExpression(method.Arguments[1])));
            method.CodeBuilder.AddStatement(new ReturnStatement());

            return type;
        }

        public static Type GenerateFor(Type interfaceType, bool isWorkspaceService)
        {
            var type = new RoslynInterfaceProxy(interfaceType)
                .GenerateCode(typeof(object), Type.EmptyTypes, new ProxyGenerationOptions
                {
                    AdditionalAttributes =
                    {
                        isWorkspaceService
                            ? new CustomAttributeBuilder(typeof(ExportWorkspaceServiceAttribute).GetConstructors().First(),
                                new object[] { interfaceType, ServiceLayer.Default} )
                            // ReSharper disable once AssignNullToNotNullAttribute
                            : new CustomAttributeBuilder(typeof(ExportAttribute).GetConstructor(new[] { typeof(Type) }),
                                new object[] { interfaceType }),
                        // ReSharper disable once AssignNullToNotNullAttribute
                        new CustomAttributeBuilder(typeof(SharedAttribute).GetConstructor(Type.EmptyTypes), new object[0])
                    }
                });
            return type;
        }

        internal static void Initialize(object o, IInterceptor interceptor, Workspace workspace)
        {
            o.GetType().GetMethod(InitializeMethod).Invoke(o, new object[] { new[] { interceptor }, workspace });
        }
    }
}