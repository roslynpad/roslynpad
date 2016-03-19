using RoslynPad.Roslyn;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class MainViewModel : NotificationObject
    {
        public const string NuGetPathVariableName = "$NuGet";

        public RoslynHost RoslynHost { get; }

        public MainViewModel()
        {
            NuGet = new NuGetViewModel();
            RoslynHost = new RoslynHost(new NuGetProvider(NuGet.GlobalPackageFolder, NuGetPathVariableName));
        }

        public NuGetViewModel NuGet { get; }

        class NuGetProvider : INuGetProvider
        {
            public NuGetProvider(string pathToRepository, string pathVariableName)
            {
                PathToRepository = pathToRepository;
                PathVariableName = pathVariableName;
            }

            public string PathToRepository { get; }
            public string PathVariableName { get; }
        }
    }
}