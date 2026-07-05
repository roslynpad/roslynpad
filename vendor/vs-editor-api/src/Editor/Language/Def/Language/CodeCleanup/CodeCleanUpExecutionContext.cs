using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    internal class CodeCleanUpExecutionContext : ICodeCleanUpExecutionContext
    {
        public CodeCleanUpExecutionContext(IUIThreadOperationContext operationContext, FixIdContainer EnabledFixIds)
        {
            Requires.NotNull(operationContext, nameof(operationContext));
            Requires.NotNull(EnabledFixIds, nameof(EnabledFixIds));
            this.OperationContext = operationContext;
            this.EnabledFixIds = EnabledFixIds;

        }

        public IUIThreadOperationContext OperationContext { get; }

        public FixIdContainer EnabledFixIds { get; }
    }
}
