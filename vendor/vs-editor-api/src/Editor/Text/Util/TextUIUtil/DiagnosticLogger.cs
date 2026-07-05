using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.UI.Utilities
{
    public static class DiagnosticLogger
    {
        private static bool WasLoggingEnabled;
        private static List<(long, string)> Log = new List<(long, string)>();

        public static bool IsLoggingEnabled(ITextView textView)
        {
            var currentValue = textView.Options.GetOptionValue(DefaultOptions.DiagnosticModeOptionId);
            if (!WasLoggingEnabled && currentValue)
            {
                Add("--- Begin new log");
            }
            if (WasLoggingEnabled != currentValue)
            {
                WasLoggingEnabled = currentValue;
            }
            return currentValue;
        }

        public static void Add(string message)
        {
            Log.Add((DateTime.Now.Ticks, message));
        }

        public static void Add(string message, object param)
        {
            Log.Add((DateTime.Now.Ticks, message + param.ToString()));
        }
    }
}
