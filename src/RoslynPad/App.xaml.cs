using System.Windows;
using RoslynPad.Properties;

namespace RoslynPad
{
    public partial class App
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Settings.Default.Save();
        }
    }
}
