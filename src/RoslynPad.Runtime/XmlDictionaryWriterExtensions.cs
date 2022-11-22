using System;
using System.Xml;

namespace RoslynPad.Runtime;

internal static class XmlDictionaryWriterExtensions
{
    public static ElementDisposer WriteObject(this XmlDictionaryWriter jsonWriter, string? name = null)
    {
        jsonWriter.WriteStartElement(name ?? "root", "");
        jsonWriter.WriteAttributeString("type", "object");
        return new ElementDisposer(jsonWriter);
    }

    public static ElementDisposer WriteArray(this XmlDictionaryWriter jsonWriter, string name)
    {
        jsonWriter.WriteStartElement(name);
        jsonWriter.WriteAttributeString("type", "array");
        return new ElementDisposer(jsonWriter);
    }

    public static void WriteProperty(this XmlDictionaryWriter jsonWriter, string name, string? value) =>
        jsonWriter.WriteElementString(name, value);

    public static void WriteProperty(this XmlDictionaryWriter jsonWriter, string name, int value)
    {
        jsonWriter.WriteStartElement(name);
        jsonWriter.WriteValue(value);
        jsonWriter.WriteEndElement();
    }

    public static void WriteProperty(this XmlDictionaryWriter jsonWriter, string name, double value)
    {
        jsonWriter.WriteStartElement(name);
        jsonWriter.WriteValue(value);
        jsonWriter.WriteEndElement();
    }

    public static void WriteProperty(this XmlDictionaryWriter jsonWriter, string name, bool value)
    {
        jsonWriter.WriteStartElement(name);
        jsonWriter.WriteValue(value);
        jsonWriter.WriteEndElement();
    }

    public struct ElementDisposer : IDisposable
    {
        private readonly XmlDictionaryWriter _writer;

        public ElementDisposer(XmlDictionaryWriter writer) => _writer = writer;

        public void Dispose() => _writer.WriteEndElement();
    }
}
