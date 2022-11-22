using static RoslynPad.Build.LibraryRef;

namespace RoslynPad.Build
{
    internal record LibraryRef(RefKind Kind, string Value, string Version)
    {
        public static LibraryRef Reference(string path) => new(RefKind.Reference, path, string.Empty);
        public static LibraryRef FrameworkReference(string id) => new(RefKind.FrameworkReference, id, string.Empty);
        public static LibraryRef PackageReference(string id, string versionRange) => new(RefKind.PackageReference, id, versionRange);

        public enum RefKind
        {
            Reference,
            FrameworkReference,
            PackageReference
        }
    }
}
