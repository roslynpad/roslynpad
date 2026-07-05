using System;
using System.Composition;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Along with <see cref="ExportImplementationAttribute"/> enables MEF proxy pattern where a single component export serves
    /// as a proxy for the best implementation selected at run time. This pattern allows component consumers to just [Import] it,
    /// hiding the complexity of selecting one of implementations.
    /// </summary>
    /// <example>
    /// A typical sample:
    ///
    /// A component contract definition:
    /// 
    /// interface IService {
    ///     void Foo();
    /// }
    ///
    /// Default implementation:
    /// 
    /// [ExportImplementation(typeof(IService))]
    /// [Name("default")]
    /// class DefaultService : IService {...}
    ///
    /// Another implementation:
    /// 
    /// [ExportImplementation(typeof(IService))]
    /// [Name("A better implementation")]
    /// [Order(Before = "default")]
    /// class AdvancedService : IService {...}
    ///
    /// A proxy:
    /// 
    /// [Export(typeof(IService))]
    /// class ProxyService : IService {
    ///    [ImportImplementations(typeof(IService))]
    ///    IEnumerable&lt;Lazy&lt;IService, Orderable>> _unorderedImplementations;
    ///    
    ///    public void Foo() {
    ///        Orderer.Order(_unorderedImplementations).FirstOrDefault()?.Value.Foo();
    ///    }
    /// }
    ///
    /// Consuming IService:
    ///
    /// [Import]
    /// IService service = null;
    /// </example>

    public class ImportImplementationsAttribute : ImportManyAttribute
    {
        /// <summary>
        /// Creates new <see cref="ImportImplementationsAttribute"/> instance.
        /// </summary>
        /// <param name="contractType">A contract type. Unused under System.Composition,
        /// where the import's item type is taken from the decorated member; kept so
        /// call sites match the original MEF v1 signature.</param>
        public ImportImplementationsAttribute(Type contractType)
            : base(ExportImplementationAttribute.ImplementationContractName)
        {
        }
    }
}
