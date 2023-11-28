[CmdletBinding()]
param (
  [Parameter(Mandatory = $true)]
  [ValidateSet('Avalonia', 'MacOSStandalone', 'Windows', 'WindowsStandalone')]
  $Mode
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Set-Location $PSScriptRoot

Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

. .\Common.ps1

Write-Host 'Building...'


switch ($Mode) {
  'Avalonia' {
    dotnet publish ..\src\RoslynPad.Avalonia
    $archiveFile = "$PSScriptRoot\RoslynPadAvalonia.zip"
  }
  'MacOSStandalone' {
    dotnet publish ..\src\RoslynPad.Avalonia --self-contained -r osx-x64
    $archiveFile = "$PSScriptRoot\RoslynPadMacOS-x64.zip"
  }
  'Windows' {
    dotnet build ..\src\RoslynPad -c Release
    $archiveFile = "$PSScriptRoot\RoslynPad.zip"
  }
  'WindowsStandalone' {
    dotnet build ..\src\RoslynPad -c Release --self-contained -r win-x64
    $archiveFile = "$PSScriptRoot\RoslynPadWindowsStandalone.zip"
  }
}

$avalonia = $Mode -eq 'Avalonia'
$rootPath = Get-PackageRoot -Avalonia:$avalonia
$files = Get-PackageFiles $rootPath

Remove-Item $archiveFile -ErrorAction Ignore

Write-Host 'Zipping...'

try {
  $archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)

  foreach ($file in $files) {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $file.Substring($rootPath.Length + 1).Replace("\", "/")) | Out-Null
    $file
  }
}
finally {
  $archive.Dispose()
}

if ($Mode -eq 'WindowsStandalone') {
  Write-Host 'Updating winget manifest...'

  $wingetManifestPath = 'winget.yaml'
  $wingetManifest = Get-Content $wingetManifestPath
  $version = [Version] (Get-RoslynPadVersion)
  $releaseVersion = $version.Minor -le 0 -and $version.Build -le 0 ? $version.Major : $version
  $hash = (Get-FileHash $archiveFile -Algorithm SHA256).Hash
  $wingetManifest = $wingetManifest -replace 'PackageVersion:.*', "PackageVersion: $version"
  $wingetManifest = $wingetManifest -replace 'InstallerUrl:.*', "InstallerUrl: https://github.com/roslynpad/roslynpad/releases/download/$releaseVersion/RoslynPadWindowsStandalone.zip"
  $wingetManifest = $wingetManifest -replace 'InstallerSha256:.*', "InstallerSha256: $hash"
  Set-Content -Path $wingetManifestPath -Value $wingetManifest
}
