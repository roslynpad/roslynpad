using System.Collections.Generic;

namespace RoslynPad.UI
{
    internal interface IPlatformsFactory
    {
        IEnumerable<ExecutionPlatform> GetExecutionPlatforms();
    }
}