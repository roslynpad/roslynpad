using System;
using System.Runtime;

namespace RoslynPad;

public partial class App
{
    private const string ProfileFileName = "RoslynPad.jitprofile";

    public App()
    {
        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile(ProfileFileName);
    }
}
