$telemetryKey = ${env:RoslynPadTelemetryKey};
if ($telemetryKey -eq $null)
{
    throw "Missing RoslynPadTelemetryKey environment variable"
}

$binPath = "$PSScriptRoot\..\src\RoslynPad.NetCore\bin\Release\netcoreapp2.2\publish"
$exclude =
@(
);

$rootPath = [IO.Path]::GetFullPath("$binPath\")

$files = get-childitem "$rootPath\*.*" -file | where { $exclude -notcontains $_.Name } | select -ExpandProperty FullName
$files += get-childitem "$rootPath\runtimes\*.*" -recurse -file | select -ExpandProperty FullName
