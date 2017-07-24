using System.Runtime.Serialization;

namespace RoslynPad.Roslyn
{
    [DataContract]
    public class NuGetConfiguration
    {
        public NuGetConfiguration(string pathToRepository, string pathVariableName)
        {
            PathToRepository = pathToRepository;
            PathVariableName = pathVariableName;
        }

        [DataMember]
        public string PathToRepository { get; private set; }

        [DataMember]
        public string PathVariableName { get; private set; }
    }
}