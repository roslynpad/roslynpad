$binPath = "..\src\RoslynPad\bin\Release"
$exclude =
@(
	"Xceed.Wpf.AvalonDock.Themes.Aero.dll",
	"Xceed.Wpf.AvalonDock.Themes.Metro.dll",
	"Xceed.Wpf.AvalonDock.Themes.VS2010.dll",
	"Xceed.Wpf.DataGrid.dll"
);
$files = get-childitem "$location\$binPath\*.dll" | select -ExpandProperty Name | where { $exclude -notcontains $_ }	
$files +=
@(
	"RoslynPad.exe",
	"RoslynPad.Host32.exe",
	"RoslynPad.Host64.exe",
	"RoslynPad.exe.config",
	"RoslynPad.Host32.exe.config",
	"RoslynPad.Host64.exe.config"
)

$configFile = "$location\$binPath\RoslynPad.exe.config"
$config = get-content $configFile
$telemetryKey = ${env:RoslynPadTelemetryKey};
$config  = $config -replace 'key="InstrumentationKey" value="[^"]*', ('key="InstrumentationKey" value="' + $telemetryKey)
set-content $configFile $config
