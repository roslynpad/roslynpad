using System.Collections.Generic;
using RoslynPad.Build;

namespace RoslynPad.UI;

internal interface IPlatformsFactory
{
    IReadOnlyList<ExecutionPlatform> GetExecutionPlatforms();

    string DotNetExecutable { get; }
}
