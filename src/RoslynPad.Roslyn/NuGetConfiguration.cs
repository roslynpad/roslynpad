using System.Diagnostics.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "Serialization")]
    public class NuGetConfiguration
    {
        public NuGetConfiguration(string pathToRepository, string pathVariableName)
        {
            PathToRepository = pathToRepository;
            PathVariableName = pathVariableName;
        }

        public string PathToRepository { get; private set; }

        public string PathVariableName { get; private set; }
    }
}