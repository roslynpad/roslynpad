using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A proxy service for exposing best implementation to the MEF composition.
    /// </summary>
    public abstract class BaseProxyService<T> where T : class
    {
        // Morgania: public so System.Composition can satisfy [ImportMany] on overrides.
        public abstract IEnumerable<Lazy<T, Orderable>> UnorderedImplementations { get; set; }

        private T bestImpl;

        protected virtual T BestImplementation
        {
            get
            {
                if (this.bestImpl == null)
                {
                    var orderedImpls = Orderer.Order(UnorderedImplementations);
                    if (orderedImpls.Count == 0)
                    {
                        throw new InvalidOperationException($"Expected to import at least one export of {typeof(T).FullName}, but got none.");
                    }

                    this.bestImpl = orderedImpls[0].Value;
                }

                return this.bestImpl;
            }
        }
    }
}
