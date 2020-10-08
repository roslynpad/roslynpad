param (
    [switch] $Avalonia,
    [switch] $Published
)

$ErrorActionPreference = 'Stop'

$telemetryKey = ${env:RoslynPadTelemetryKey};
if ($null -eq $telemetryKey) {
    throw "Missing RoslynPadTelemetryKey environment variable"
}

$project = if ($Avalonia) { 'RoslynPad.Avalonia' } else { 'RoslynPad' }
$path = if ($Published) { 'bin\Release\netcoreapp3.1\win-x64\publish' } else { 'bin\Release\netcoreapp3.1' }
$rootPath = [IO.Path]::GetFullPath("$PSScriptRoot\..\src\$project\$path")
$exclude = @();

$files = get-childitem "$rootPath\*.*" -file | where { $exclude -notcontains $_.Name } | select -ExpandProperty FullName
if (Test-Path "$rootPath\runtimes") {
    $files += get-childitem "$rootPath\runtimes\*.*" -recurse -file | select -ExpandProperty FullName
}
