Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

$location = Get-Location
$archiveFile = "$location\RoslynPad.zip"
Remove-Item $archiveFile -ErrorAction Ignore
try
{
	$archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)
	$exclude =
	@(
		"Microsoft.AI.Agent.Intercept.dll",
		"Microsoft.AI.DependencyCollector.dll",
		"Microsoft.AI.PerfCounterCollector.dll",
		"Microsoft.AI.WindowsServer.dll",
		"Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll",
		"Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll",
		"Xceed.Wpf.AvalonDock.Themes.Aero.dll",
		"Xceed.Wpf.AvalonDock.Themes.Metro.dll",
		"Xceed.Wpf.AvalonDock.Themes.VS2010.dll",
		"Xceed.Wpf.DataGrid.dll"
	);
	$files = ls "$location\RoslynPad\bin\Release\*.dll" | select -ExpandProperty Name | where { $exclude -notcontains $_ }	
	$files +=
	@(
		"RoslynPad.exe",
		"RoslynPad.Host32.exe",
		"RoslynPad.Host64.exe",
		"RoslynPad.exe.config"
	)

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
