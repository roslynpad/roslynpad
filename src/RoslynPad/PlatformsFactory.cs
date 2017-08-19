using System.Composition;
using System.Collections.Generic;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            yield return new ExecutionPlatform("Desktop x86", "RoslynPad.Host32.exe", string.Empty);
            yield return new ExecutionPlatform("Desktop x64", "RoslynPad.Host64.exe", string.Empty);
        }
    }
}