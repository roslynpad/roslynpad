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
		"Castle.Core.dll",
		"Castle.DynamicProxy2.dll",
		"ICSharpCode.AvalonEdit.dll",
		"Microsoft.CodeAnalysis.CSharp.dll",
		"Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.CSharp.Features.dll",
		"Microsoft.CodeAnalysis.CSharp.Scripting.dll",
		"Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
		"Microsoft.CodeAnalysis.dll",
		"Microsoft.CodeAnalysis.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.EditorFeatures.Text.dll",
		"Microsoft.CodeAnalysis.Features.dll",
		"Microsoft.CodeAnalysis.Scripting.dll",
		"Microsoft.CodeAnalysis.Workspaces.Desktop.dll",
		"Microsoft.CodeAnalysis.Workspaces.dll",
		"RoslynPad.exe",
		"RoslynPad.exe.config",
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
