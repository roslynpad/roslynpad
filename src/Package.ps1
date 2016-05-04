Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

$location = Get-Location
$archiveFile = "$location\RoslynPad.zip"
Remove-Item $archiveFile -ErrorAction Ignore
try
{
	$archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)
	$files =
	@(
		"RoslynPad.exe",
		"RoslynPad.Host32.exe",
		"RoslynPad.Host64.exe",
		"AvalonLibrary.dll",
		"ICSharpCode.AvalonEdit.dll",
		"Microsoft.AI.Agent.Intercept.dll",
		"Microsoft.AI.DependencyCollector.dll",
		"Microsoft.AI.PerfCounterCollector.dll",
		"Microsoft.AI.ServerTelemetryChannel.dll",
		"Microsoft.AI.WindowsServer.dll",
		"Microsoft.ApplicationInsights.dll",
		"Microsoft.CodeAnalysis.CSharp.dll",
		"Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.CSharp.Features.dll",
		"Microsoft.CodeAnalysis.CSharp.Scripting.dll",
		"Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
		"Microsoft.CodeAnalysis.dll",
		"Microsoft.CodeAnalysis.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.Features.dll",
		"Microsoft.CodeAnalysis.Scripting.dll",
		"Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.Workspaces.Desktop.dll",
		"Microsoft.CodeAnalysis.Workspaces.dll",
		"Microsoft.Web.XmlTransform.dll",
		"Newtonsoft.Json.dll",
		"NuGet.Client.dll",
		"NuGet.Commands.dll",
		"NuGet.Configuration.dll",
		"NuGet.ContentModel.dll",
		"NuGet.Core.dll",
		"NuGet.DependencyResolver.Core.dll",
		"NuGet.DependencyResolver.dll",
		"NuGet.Frameworks.dll",
		"NuGet.LibraryModel.dll",
		"NuGet.Logging.dll",
		"NuGet.PackageManagement.dll",
		"NuGet.Packaging.Core.dll",
		"NuGet.Packaging.Core.Types.dll",
		"NuGet.Packaging.dll",
		"NuGet.ProjectManagement.dll",
		"NuGet.ProjectModel.dll",
		"NuGet.Protocol.Core.Types.dll",
		"NuGet.Protocol.Core.v2.dll",
		"NuGet.Protocol.Core.v3.dll",
		"NuGet.Repositories.dll",
		"NuGet.Resolver.dll",
		"NuGet.RuntimeModel.dll",
		"NuGet.Versioning.dll",
		"RoslynETAHost.dll",
		"RoslynPad.Common.dll",
		"RoslynPad.RoslynEditor.dll",
		"System.AppContext.dll",
		"System.Collections.Immutable.dll",
		"System.Composition.AttributedModel.dll",
		"System.Composition.Convention.dll",
		"System.Composition.Hosting.dll",
		"System.Composition.Runtime.dll",
		"System.Composition.TypedParts.dll",
		"System.Diagnostics.StackTrace.dll",
		"System.IO.FileSystem.dll",
		"System.IO.FileSystem.Primitives.dll",
		"System.Reflection.Metadata.dll",
		"Xceed.Wpf.AvalonDock.dll",
		"Xceed.Wpf.Toolkit.dll"
	);

	foreach ($file in $files)
	{
		$target = "$location\RoslynPad\bin\Release\$file"
		[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $target, $file) | Out-Null
		$file
	}
}
finally
{
	$archive.Dispose()
}
