using System;
using System.Collections.Generic;
using RoslynPad.Build;

namespace RoslynPad.UI
{
    internal interface IPlatformsFactory
    {
        IEnumerable<ExecutionPlatform> GetExecutionPlatforms();

        string DotNetExecutable { get; }

        event Action Changed;
    }
}