using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Editor;

public interface IContextActionProvider
{
    Task<IEnumerable<object>> GetActions(int offset, int length, CancellationToken cancellationToken);

    ICommand? GetActionCommand(object action);
}
