using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    struct CompletionAvailabilityCacheKey : IEquatable<CompletionAvailabilityCacheKey>
    {
        public IContentType ContentType { get; }
        public ITextViewRoleSet Roles { get; }

        public CompletionAvailabilityCacheKey(IContentType contentType, ITextViewRoleSet roles)
        {
            ContentType = contentType;
            Roles = roles;
        }

        bool IEquatable<CompletionAvailabilityCacheKey>.Equals(CompletionAvailabilityCacheKey other) =>
            ContentType.Equals(other.ContentType) && (Roles == null || Roles.Equals(other.Roles));

        public override bool Equals(object other) =>
            (other is CompletionAvailabilityCacheKey otherKey) ? ((IEquatable<CompletionAvailabilityCacheKey>)this).Equals(otherKey) : false;

        public static bool operator ==(CompletionAvailabilityCacheKey left, CompletionAvailabilityCacheKey right) => left.Equals(right);

        public static bool operator !=(CompletionAvailabilityCacheKey left, CompletionAvailabilityCacheKey right) => !(left == right);

        public override int GetHashCode() => (ContentType, Roles).GetHashCode();
    }
}
