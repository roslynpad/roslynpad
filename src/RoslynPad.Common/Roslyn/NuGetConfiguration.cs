using System;

namespace RoslynPad.Roslyn
{
    [Serializable]
    public class NuGetConfiguration
    {
        public NuGetConfiguration(string pathToRepository, string pathVariableName)
        {
            PathToRepository = pathToRepository;
            PathVariableName = pathVariableName;
        }

        public string PathToRepository { get; }

        public string PathVariableName { get; }
    }
}