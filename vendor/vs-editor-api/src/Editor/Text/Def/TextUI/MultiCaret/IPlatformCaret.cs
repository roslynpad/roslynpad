using System;

namespace Microsoft.VisualStudio.Text
{
    public interface IPlatformCaret
    {
        TimeSpan BlinkTimeOn { get; }
        TimeSpan BlinkTimeOff { get; }

        event EventHandler<EventArgs> BlinkTimeChanged;
    }
}