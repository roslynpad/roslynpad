using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Service to register, unregister, and enumerate available code clean up fixers.
    /// The methods and properties on this class are thread safe.
    ///
    /// This MEF component should be imported as
    /// [Import]
    /// private ICodeCleanUpFixerRegistrationService FixerRegistrationService;
    /// </summary>
    public interface ICodeCleanUpFixerRegistrationService
    {
        /// <summary>
        /// Register a fixer provider which can participate in code cleanup.
        /// If the same instance of the provider is registered again only one instance will be kept
        /// Fixer providers should be registered when the language service or extension is initialzed
        /// </summary>
        /// <param name="fixerInstance">Instance which will be called to invoke the fixer</param>
        bool TryRegisterFixerProvider(ICodeCleanUpFixerProvider fixerProvider);

        /// <summary>
        /// Unregister a fixer provider
        /// Fixer providers should be unregistered when the language service or extension is disposed
        /// </summary>
        /// <param name="fixerInstance">Instance to un-register</param>
        bool TryUnRegisterFixerProvider(ICodeCleanUpFixerProvider fixerProvider);
               
        /// <summary>
        /// Gets a snapshot of the current set of registered fixer providers
        /// </summary>
        IReadOnlyCollection<ICodeCleanUpFixerProvider> RegisteredFixerProviders { get; }
                
        /// <summary>
        /// Gets the set of all enabled fix ids
        /// </summary>
        FixIdContainer EnabledFixIds { get; }
    }
}
