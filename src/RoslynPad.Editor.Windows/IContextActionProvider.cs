using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoslynPad.Editor.Windows
{
    internal interface IContextActionProvider
    {
        Task<IEnumerable<object>> GetActions(int offset, int length, CancellationToken cancellationToken);

        bool IsSameAction(object a, object b);

        ICommand GetActionCommand(object action);
    }
}