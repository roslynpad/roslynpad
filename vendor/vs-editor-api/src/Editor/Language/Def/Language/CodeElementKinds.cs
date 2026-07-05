using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents the kind of code elements in a document.
    /// </summary>
    [Flags]
    public enum CodeElementKinds
    {
        // enumeration values arranged in this way:
        // bit 31 - bit 16: reserved for expansion
        // bit 15 - bit 12: type containers
        // bit 11 - bit 8 : types
        // bit 7 -  bit 0 : type members

        /// <summary>
        /// Unspecified kind.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// Invalid kind.
        /// </summary>
        Invalid = -1,

        /// <summary>
        /// Kind is a type container.
        /// </summary>
        /// <remarks>
        /// A type container can be any of <see cref="File"/>, <see cref="Module"/>, <see cref="Package"/>, or <see cref="Namespace"/>.
        /// Code should use one of the concrete values if needed.
        /// </remarks>
        Container = 0xf000,
        /// <summary>
        /// Kind is a file.
        /// </summary>
        File = 0x8000,
        /// <summary>
        /// Kind is a module.
        /// </summary>
        Module = 0x4000,
        /// <summary>
        /// Kind is a package.
        /// </summary>
        Package = 0x2000,
        /// <summary>
        /// Kind is a namespace.
        /// </summary>
        Namespace = 0x1000,

        /// <summary>
        /// Kind is a type (<see cref="Class"/>, <see cref="Interface"/>, <see cref="Struct"/>, or <see cref="Enum"/>).
        /// </summary>
        Type = 0x0f00,
        /// <summary>
        /// Kind is a class.
        /// </summary>
        Class = 0x0800,
        /// <summary>
        /// Kind is an interface.
        /// </summary>
        Interface = 0x0400,
        /// <summary>
        /// Kind is a struct.
        /// </summary>
        Struct = 0x0200,
        /// <summary>
        /// Kind is an enum.
        /// </summary>
        Enum = 0x0100,

        /// <summary>
        /// Kind is a type member.
        /// </summary>
        /// <remarks>
        /// A <see cref="Member"/> kind can be any of
        /// <see cref="Method"/>
        /// <see cref="Property"/>
        /// <see cref="Event"/>
        /// <see cref="Field"/>
        /// <see cref="Constructor"/>
        /// <see cref="Function"/>
        /// </remarks>
        Member = 0x00ff,
        /// <summary>
        /// Kind is a method.
        /// </summary>
        Method = 0x0080,
        /// <summary>
        /// Kind is a property.
        /// </summary>
        Property = 0x0040,
        /// <summary>
        /// Kind is an event.
        /// </summary>
        Event = 0x0020,
        /// <summary>
        /// Kind is a field.
        /// </summary>
        Field = 0x0010,
        /// <summary>
        /// Kind is a constructor.
        /// </summary>
        Constructor = 0x0008,
        /// <summary>
        /// Kind is a function.
        /// </summary>
        Function = 0x0004,
    }
}
