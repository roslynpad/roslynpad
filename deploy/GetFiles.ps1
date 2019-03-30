$telemetryKey = ${env:RoslynPadTelemetryKey};
if ($telemetryKey -eq $null)
{
    throw "Missing RoslynPadTelemetryKey environment variable"
}

$binPath = "..\src\RoslynPad\bin\Release\netcoreapp3.0\win"
$exclude =
@(
	"Xceed.Wpf.AvalonDock.Themes.Aero.dll",
	"Xceed.Wpf.AvalonDock.Themes.Metro.dll",
	"Xceed.Wpf.AvalonDock.Themes.VS2010.dll",
	"Xceed.Wpf.DataGrid.dll"
);

$rootPath = [IO.Path]::GetFullPath("$location\$binPath\")

$files = get-childitem "$rootPath\*.*" -file | where { $exclude -notcontains $_.Name } | select -ExpandProperty FullName
$files += get-childitem "$rootPath\runtimes\*.*" -recurse -file | select -ExpandProperty FullName

$configFile = "$location\$binPath\RoslynPad.exe.config"
$config = get-content $configFile
$config  = $config -replace 'key="InstrumentationKey" value="[^"]*', ('key="InstrumentationKey" value="' + $telemetryKey)
set-content $configFile $config
