using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.UnitTests")]
[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.IntegrationTests")]
[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.UnitTestHelper")]
[assembly: InternalsVisibleTo("Morgania.Language.UnitTests")]

// Bug #512117: Remove compatibility shims for 2nd gen. Quick Info APIs.
// MS.VS.Text.Internal has references to WPF, so, we'll stash our cross plat 'internal' interfaces directly
// in the Language assembly and make them internal with internals visible only to our trusted VS assemblies.
// When the compat shims are removed, we can probably get rid of these.
[assembly: InternalsVisibleTo("Morgania.Language.Implementation")]
[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.Implementation")]
[assembly: InternalsVisibleTo("Morgania.Platform.VSEditor")]
[assembly: InternalsVisibleTo("Morgania.Editor.Implementation")]
[assembly: InternalsVisibleTo("Morgania.CodeLens.Service")]
[assembly: InternalsVisibleTo("Morgania.UI.Text.Cocoa.View.Implementation")]
[assembly: InternalsVisibleTo("Morgania.Platform.VSEditor")]

// InternalsVisibleTo for VS for Mac implementation assembly:
[assembly: InternalsVisibleTo("Morgania.Text.Implementation, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e57febc1f220077550a65e338d3d15d7cbd189cf4f62f7c3829dcb2f8441a6c40631d172e3deb4dc0bb7237b44ec9daeb9bd7d72c3d64c4f52b968795443cb58bc341583c29440345b8c35f72f6a31aecb2903376136f8fc35779bb422eb643f8668fa6605c697bff927e3bb10745328ff878bd1b7e42bbcb839f04baa8460bd")]

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
