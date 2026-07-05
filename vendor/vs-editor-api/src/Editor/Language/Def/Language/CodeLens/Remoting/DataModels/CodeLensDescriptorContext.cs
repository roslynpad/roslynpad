using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Additional, mutable context for a <see cref="CodeLensDescriptor"/>, passed to data point
    /// providers alongside the descriptor.
    /// </summary>
    public sealed class CodeLensDescriptorContext
    {
        public CodeLensDescriptorContext()
            : this(null, null)
        {
        }

        public CodeLensDescriptorContext(Span? applicableSpan, IDictionary<string, object> properties)
        {
            ApplicableSpan = applicableSpan;
            Properties = properties ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// The span of the element this code lens applies to, if known.
        /// </summary>
        public Span? ApplicableSpan { get; }

        /// <summary>
        /// Additional provider-specific properties.
        /// </summary>
        public IDictionary<string, object> Properties { get; }
    }
}
