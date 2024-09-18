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

function Build-MacOSPackage($PackageName, $RootPath) {
  if (!(Get-Command appdmg -ErrorAction Ignore)) {
    throw 'Missing appdmg. Install using "npm install -g appdmg"'
  }

  $dmgSpecPath = Join-Path $RootPath 'dmgspec.json'
  Copy-Item (Join-Path $PSScriptRoot 'dmgspec.json') $dmgSpecPath -Force

  $dmgPath = Join-Path $PSScriptRoot "RoslynPad-$PackageName.dmg"
  Remove-Item $dmgPath -ErrorAction Ignore
  Write-Host "Creating package $dmgPath..."
  appdmg $dmgSpecPath $dmgPath
}

function Build-MacOSLinuxPackage($PackageName, $RootPath, [switch] $IsMacOSPackage) {
  Push-Location $RootPath
  try {
    $archiveFile = Join-Path $PSScriptRoot "RoslynPad-$PackageName.tgz"
    Remove-Item $archiveFile -ErrorAction Ignore
    Write-Host "Compressing $PackageName into $archiveFile..."
    tar -czf $archiveFile ($IsMacOSPackage ? 'RoslynPad.app' : '*')
  }
  finally {
    Pop-Location
  }
}

function Build-WindowsPackages($PackageName, $RootPath) {
  $archiveFile = Join-Path $PSScriptRoot "RoslynPad-$PackageName.zip"
  Remove-Item $archiveFile -ErrorAction Ignore
  Write-Host "Compressing $PackageName into $archiveFile..."

  $files = Get-PackageFiles $RootPath

  try {
    $archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)

    foreach ($file in $files) {
      [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $file.Substring($RootPath.Length + 1).Replace("\", "/")) | Out-Null
      Write-Verbose "Compressing $file"
    }
  }
  finally {
    $archive.Dispose()
  }

  Write-Host 'Updating winget manifest...'

  $wingetManifestPath = "winget-$PackageName.yaml"
  $wingetManifest = Get-Content $wingetManifestPath
  $version = [Version] (Get-RoslynPadVersion)
  $releaseVersion = $version.Minor -le 0 -and $version.Build -le 0 ? $version.Major : $version
  $hash = (Get-FileHash $archiveFile -Algorithm SHA256).Hash
  $wingetManifest = $wingetManifest -replace 'PackageVersion:.*', "PackageVersion: $version"
  $wingetManifest = $wingetManifest -replace 'InstallerUrl:.*', "InstallerUrl: https://github.com/roslynpad/roslynpad/releases/download/$releaseVersion/RoslynPad-$PackageName.zip"
  $wingetManifest = $wingetManifest -replace 'InstallerSha256:.*', "InstallerSha256: $hash"
  Set-Content -Path $wingetManifestPath -Value $wingetManifest

  ./CreateAppxPackage.ps1 -PackageName $PackageName -RootPath $RootPath
}

function Build-Package($PackageName, $RuntimeIdentifier) {
  Write-Host "Building $PackageName..."

  $isWindowsPackage = $RuntimeIdentifier -like 'win-*'
  $buildPath = Join-Path '..' 'src' ($isWindowsPackage ? 'RoslynPad' : 'RoslynPad.Avalonia')
  dotnet publish $buildPath -r $RuntimeIdentifier

  $rootPath = Get-PackageRoot -RuntimeIdentifier $RuntimeIdentifier

  if ($RuntimeIdentifier -like 'osx-*') {
    Build-MacOSPackage $PackageName $rootPath
  }
  elseif ($RuntimeIdentifier -like 'linux-*') {
    Build-MacOSLinuxPackage $PackageName $rootPath
  }
  elseif ($isWindowsPackage) {
    Build-WindowsPackages $PackageName $rootPath
  }
}

if ($IsMacOS) {
  Build-Package -PackageName 'macos-x64' -RuntimeIdentifier 'osx-x64'
  Build-Package -PackageName 'macos-arm64' -RuntimeIdentifier 'osx-arm64'
  Build-Package -PackageName 'linux-x64' -RuntimeIdentifier 'linux-x64'
  Build-Package -PackageName 'linux-arm64' -RuntimeIdentifier 'linux-arm64'
}
elseif ($IsWindows) {
  Build-Package -PackageName 'windows-x64' -RuntimeIdentifier 'win-x64'
  Build-Package -PackageName 'windows-arm64' -RuntimeIdentifier 'win-arm64'
}
