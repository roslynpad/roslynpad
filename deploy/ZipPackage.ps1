PARAM (
  [Switch]
  $Avalonia
)

$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

. .\Common.ps1

Write-Host 'Building...'

if ($Avalonia) {
  dotnet build ..\src\RoslynPad.Avalonia -c Release
  $archiveFile = "$PSScriptRoot\RoslynPadAvalonia.zip"
}
else {
  dotnet build ..\src\RoslynPad -c Release
  $archiveFile = "$PSScriptRoot\RoslynPad.zip"
}

$rootPath = Get-PackageRoot -Avalonia:$Avalonia
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

if (!$Avalonia) {
  Write-Host 'Updating winget manifest...'

  $wingetManifestPath = 'winget.yaml'
  $wingetManifest = Get-Content $wingetManifestPath
  $version = [Version] (Get-RoslynPadVersion)
  $releaseVersion = $version.Minor -le 0 -and $version.Build -le 0 ? $version.Major : $version
  $hash = (Get-FileHash $archiveFile -Algorithm SHA256).Hash
  $wingetManifest = $wingetManifest -replace 'PackageVersion:.*', "PackageVersion: $version"
  $wingetManifest = $wingetManifest -replace 'InstallerUrl:.*', "InstallerUrl: https://github.com/roslynpad/roslynpad/releases/download/$releaseVersion/RoslynPad.zip"
  $wingetManifest = $wingetManifest -replace 'InstallerSha256:.*', "InstallerSha256: $hash"
  Set-Content -Path $wingetManifestPath -Value $wingetManifest
}
