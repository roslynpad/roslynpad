using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace RoslynPad.Runtime
{
    /// <summary>
    /// Parses a deps JSON file.
    /// </summary>
    internal class DepsParser : IDisposable
    {
        private readonly FileStream _stream;
        private readonly XmlDictionaryReader _reader;
        private readonly string _rootLibraryPath;

        public DepsParser(string depsFile, string rootLibraryPath)
        {
            _stream = File.OpenRead(depsFile);
            _reader = JsonReaderWriterFactory.CreateJsonReader(_stream, XmlDictionaryReaderQuotas.Max);
            _rootLibraryPath = rootLibraryPath;
        }

        public IReadOnlyDictionary<string, string> ParseRuntimeAssemblies()
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // root
            _reader.ReadStartElement();

            ReadToProperty("targets", validateExists: true);

            // first target, e.g. .NETCoreApp
            _reader.ReadStartElement();

            while (_reader.Read() && IsStartObject())
            {
                var idVersion = ReadPropertyName().ToLowerInvariant();

                _reader.ReadStartElement();

                if (IsEndObject() || !ReadToProperty("runtime") || IsEndObject())
                {
                    continue;
                }

                while (_reader.Read() && IsStartObject())
                {
                    var filePath = ReadPropertyName();
                    var assemblyName = Path.GetFileNameWithoutExtension(filePath);
                    dictionary[assemblyName] = Path.Combine(_rootLibraryPath, idVersion, filePath);

                    _reader.Skip();
                }

                if (IsEndObject())
                {
                    continue;
                }

                _reader.Skip();
            }

            return new ReadOnlyDictionary<string, string>(dictionary);
        }

        private bool IsStartObject()
        {
            return _reader.NodeType == XmlNodeType.Element;
        }

        private bool IsEndObject()
        {
            return _reader.NodeType == XmlNodeType.EndElement;
        }

        private string ReadPropertyName()
        {
            return _reader.Name == "a:item" ? _reader.GetAttribute(1) : _reader.LocalName;
        }

        private bool ReadToProperty(string propertyName, bool validateExists = false)
        {
            while (!IsValidProperty(propertyName) &&
                   _reader.NodeType != XmlNodeType.EndElement &&
                   !_reader.EOF)
            {
                _reader.Skip();
            }

            var exists = IsValidProperty(propertyName);

            if (validateExists && !exists)
            {
                throw new InvalidOperationException($"depsfile: '{propertyName}' property not found");
            }

            return exists;

            bool IsValidProperty(string propertyName)
            {
                return _reader.NodeType == XmlNodeType.Element &&
                       _reader.LocalName == propertyName;
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
            _stream.Dispose();
        }
    }
}
