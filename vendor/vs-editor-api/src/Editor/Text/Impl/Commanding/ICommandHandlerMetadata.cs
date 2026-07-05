using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    public interface ICommandHandlerMetadata : IOrderable, IContentTypeMetadata
    {
        [DefaultValue(null)] // [TextViewRole] is optional
        IEnumerable<string> TextViewRoles { get; }
    }


    /// <summary>
    /// Concrete metadata view for <see cref="ICommandHandlerMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class CommandHandlerMetadata : ICommandHandlerMetadata
    {
        public CommandHandlerMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.Name = Microsoft.VisualStudio.Utilities.MetadataValue.Get<string>(data, nameof(Name));
            this.Before = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(Before));
            this.After = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(After));
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
            this.TextViewRoles = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(TextViewRoles));
        }

        public string Name { get; }
        public System.Collections.Generic.IEnumerable<string> Before { get; }
        public System.Collections.Generic.IEnumerable<string> After { get; }
        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
        public System.Collections.Generic.IEnumerable<string> TextViewRoles { get; }
    }
}
