using System.Composition;
using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(ITelemetryProvider)), Shared]
internal class TelemetryProvider : TelemetryProviderBase
{
}
