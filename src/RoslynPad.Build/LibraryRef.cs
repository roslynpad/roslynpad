using System;
using System.Collections.Generic;

namespace RoslynPad.NuGet
{
    internal class LibraryRef : IEquatable<LibraryRef>
    {
        private LibraryRef(RefKind kind, string value, string version)
        {
            Kind = kind;
            Value = value;
            Version = version;
        }

        public static LibraryRef Reference(string path) => new(RefKind.Reference, path, string.Empty);
        public static LibraryRef FrameworkReference(string id) => new(RefKind.FrameworkReference, id, string.Empty);
        public static LibraryRef PackageReference(string id, string versionRange) => new(RefKind.PackageReference, id, versionRange);

        public override bool Equals(object? obj)
        {
            return Equals(obj as LibraryRef);
        }

        public bool Equals(LibraryRef? other)
        {
            return other != null &&
                   Kind == other.Kind &&
                   Value == other.Value &&
                   Version == other.Version;
        }

        public override int GetHashCode()
        {
            var hashCode = -282739388;
            hashCode = hashCode * -1521134295 + Kind.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
        }

        public RefKind Kind { get; }
        public string Value { get; }
        public string Version { get; }

        public enum RefKind
        {
            Reference,
            FrameworkReference,
            PackageReference
        }
    }
}
