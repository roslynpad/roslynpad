using System;
using System.IO;
using System.Reflection;
using System.Runtime;

namespace RoslynPad
{
    public partial class App
    {
        private const string ProfileFileName = "JitProfile.profile";
        public App()
        {
            ProfileOptimization.SetProfileRoot(AppDomain.CurrentDomain.BaseDirectory);
            ProfileOptimization.StartProfile(ProfileFileName);
        }
    }
}
