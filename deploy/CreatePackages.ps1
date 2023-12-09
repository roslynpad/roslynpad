[CmdletBinding()]
param (
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Set-Location $PSScriptRoot

Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

. ./Common.ps1

function Get-PackageRoot {
  param (
    $RuntimeIdentifier
  )

  $project = 'RoslynPad.Avalonia'
  $targetFramework = Get-TargetFramework
  if ($RuntimeIdentifier -like 'win-*') {
    $project = 'RoslynPad'
    $path = Join-Path 'bin' 'Release' "$targetFramework-windows" $RuntimeIdentifier 'publish'
  }
  elseif ($RuntimeIdentifier -like 'osx-*') {
    $path = Join-Path 'bin' 'Release' "$targetFramework-macos" $RuntimeIdentifier
  }
  else {
    $path = Join-Path 'bin' 'Release' $targetFramework $RuntimeIdentifier 'publish'
  }

  return (Resolve-Path (Join-Path $PSScriptRoot '..' 'src' $project $path)).Path
}

function Build-Package($PackageName, $RuntimeIdentifier) {
  Write-Host "Building $PackageName..."

  $isWindowsPackage = $RuntimeIdentifier -like 'win-*'
  $buildPath = Join-Path '..' 'src' ($isWindowsPackage ? 'RoslynPad' : 'RoslynPad.Avalonia')

  dotnet publish $buildPath -r $RuntimeIdentifier
  $archiveFile = Join-Path $PSScriptRoot "RoslynPad-$PackageName.zip"
  $rootPath = Get-PackageRoot -RuntimeIdentifier $RuntimeIdentifier

  Remove-Item $archiveFile -ErrorAction Ignore

  Write-Host "Zipping $PackageName into $archiveFile..."

  if ($RuntimeIdentifier -like 'osx-*') {
    Push-Location $rootPath
    zip -r $archiveFile 'RoslynPad.app/'
    Pop-Location
  }
  elseif ($RuntimeIdentifier -like 'linux-*') {
    Push-Location $rootPath
    zip -D $archiveFile *
    Pop-Location
  }
  elseif ($isWindowsPackage) {
    $files = Get-PackageFiles $rootPath

    try {
      $archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)

      foreach ($file in $files) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $file.Substring($rootPath.Length + 1).Replace("\", "/")) | Out-Null
        Write-Verbose "Compressing $file"
      }
    }
    finally {
      $archive.Dispose()
    }

    Write-Host 'Updating winget manifest...'

    $wingetManifestPath = 'winget.yaml'
    $wingetManifest = Get-Content $wingetManifestPath
    $version = [Version] (Get-RoslynPadVersion)
    $releaseVersion = $version.Minor -le 0 -and $version.Build -le 0 ? $version.Major : $version
    $hash = (Get-FileHash $archiveFile -Algorithm SHA256).Hash
    $wingetManifest = $wingetManifest -replace 'PackageVersion:.*', "PackageVersion: $version"
    $wingetManifest = $wingetManifest -replace 'InstallerUrl:.*', "InstallerUrl: https://github.com/roslynpad/roslynpad/releases/download/$releaseVersion/RoslynPad-$PackageName.zip"
    $wingetManifest = $wingetManifest -replace 'InstallerSha256:.*', "InstallerSha256: $hash"
    Set-Content -Path $wingetManifestPath -Value $wingetManifest

    ./AppxPackage.ps1 -RootPath $rootPath
  }
}

if ($IsMacOS) {
  Build-Package -PackageName 'macos-x64' -RuntimeIdentifier 'osx-x64'
  Build-Package -PackageName 'macos-arm64' -RuntimeIdentifier 'osx-arm64'
  Build-Package -PackageName 'linux-x64' -RuntimeIdentifier 'linux-x64'
}
elseif ($IsWindows) {
  Build-Package -PackageName 'windows-x64' -RuntimeIdentifier 'win-x64'
}
