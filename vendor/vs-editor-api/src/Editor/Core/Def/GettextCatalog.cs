// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.VisualStudio.UI
{
    public static class GettextCatalog
    {
        static bool initialized;

        static Func<string, string> getStringHandler;
        static Func<string, object [], string> getStringFormatHandler;

        public static void Initialize(
            Func<string, string> getStringHandler,
            Func<string, object[], string> getStringFormatHandler)
        {
            if (initialized)
                return;

            GettextCatalog.getStringHandler = getStringHandler
                ?? throw new ArgumentNullException(nameof(getStringHandler));

            GettextCatalog.getStringFormatHandler = getStringFormatHandler
                ?? throw new ArgumentNullException(nameof(getStringFormatHandler));

            initialized = true;
        }

        public static string GetString(string message)
        {
            if (getStringHandler != null)
                return getStringHandler(message);

            return message;
        }

        public static string GetString(string format, params object[] args)
        {
            if (getStringFormatHandler != null)
                return getStringFormatHandler(format, args);

            return string.Format(format, args);
        }
    }
}