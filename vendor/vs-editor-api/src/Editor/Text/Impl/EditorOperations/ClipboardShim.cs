//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored, WPF-shaped clipboard seam for the vendored editor operations.
//  EditorOperations.cs was written against the WPF static Clipboard/DataObject API;
//  Avalonia's clipboard is asynchronous and reachable only through a TopLevel, so the
//  view layer (M2) is expected to install a real provider via Clipboard.SetProvider.
//  The default in-process store keeps the operations functional and deterministic in
//  headless tests. Types are internal and shadow Avalonia.Input names on purpose so
//  the vendored call sites compile unchanged.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    internal interface IDataObject
    {
        bool GetDataPresent(Type format);
        bool GetDataPresent(string format);
        object GetData(string format);
    }

    internal static class DataFormats
    {
        public const string UnicodeText = "UnicodeText";
        public const string Text = "Text";
        public const string Rtf = "Rich Text Format";
    }

    internal sealed class DataObject : IDataObject
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.Ordinal);

        public void SetText(string text)
        {
            _data[DataFormats.UnicodeText] = text;
            _data[DataFormats.Text] = text;
        }

        public void SetData(string format, object value)
        {
            _data[format] = value;
        }

        public bool GetDataPresent(Type format)
        {
            // The operations layer only ever probes for string content.
            return format == typeof(string) && _data.ContainsKey(DataFormats.UnicodeText);
        }

        public bool GetDataPresent(string format) => _data.ContainsKey(format);

        public object GetData(string format)
        {
            return _data.TryGetValue(format, out object value) ? value : null;
        }
    }

    /// <summary>
    /// WPF-shaped clipboard access. The host installs a platform provider; without one,
    /// an in-process store is used (sufficient for cut/copy/paste within one editor).
    /// </summary>
    internal static class Clipboard
    {
        public interface IProvider
        {
            IDataObject GetDataObject();
            void SetDataObject(IDataObject data, bool copy);
            bool ContainsText();
        }

        private sealed class InProcessProvider : IProvider
        {
            private IDataObject _current;

            public IDataObject GetDataObject() => _current;

            public void SetDataObject(IDataObject data, bool copy) => _current = data;

            public bool ContainsText() => _current != null && _current.GetDataPresent(typeof(string));
        }

        private static IProvider s_provider = new InProcessProvider();

        public static void SetProvider(IProvider provider)
        {
            s_provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static IDataObject GetDataObject() => s_provider.GetDataObject();

        public static void SetDataObject(IDataObject data, bool copy) => s_provider.SetDataObject(data, copy);

        public static bool ContainsText() => s_provider.ContainsText();
    }
}
