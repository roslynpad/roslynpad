[CmdletBinding()]
param (
  # Developer Team ID. When omitted, the script auto-detects the single Developer ID
  # Application certificate in the keychain and fails if more than one is present.
  [string] $SigningIdentity,

  # Name of the notarytool keychain profile created via
  # `xcrun notarytool store-credentials`. When omitted, notarization is skipped.
  [string] $NotaryProfile = 'notarization'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Set-Location $PSScriptRoot

Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

. ./Common.ps1

$MacOSHashes = @{}

function Get-PackageRoot {
  param (
    $RuntimeIdentifier
  )

  $project = 'RoslynPad'
  $targetFramework = Get-TargetFramework
  if ($RuntimeIdentifier -like 'osx-*') {
    $path = Join-Path 'bin' 'Release' "$targetFramework-macos" $RuntimeIdentifier
  }
  else {
    $path = Join-Path 'bin' 'Release' $targetFramework $RuntimeIdentifier 'publish'
  }

  return (Resolve-Path (Join-Path $PSScriptRoot '..' 'src' $project $path)).Path
}

function Notarize-MacOSDmg($DmgPath, $Identity, $Profile) {
  codesign --force --timestamp --sign $Identity $DmgPath

  Write-Host "Submitting $DmgPath to Apple notary service..."
  $submission = xcrun notarytool submit $DmgPath --keychain-profile $Profile --wait --output-format json | ConvertFrom-Json
  if ($submission.status -ne 'Accepted') {
    Write-Host "Notarization status: $($submission.status). Fetching log..."
    xcrun notarytool log $submission.id --keychain-profile $Profile
    throw "Notarization failed for $DmgPath (id: $($submission.id))"
  }

  Write-Host "Stapling notarization ticket..."
  xcrun stapler staple $DmgPath
  xcrun stapler validate $DmgPath
}

function Get-MacOSSigningIdentity {
  $matches = security find-identity -v -p codesigning |
    Select-String -Pattern '"Developer ID Application:[^"]*\(([A-Z0-9]{10,})\)"' -AllMatches |
    ForEach-Object { $_.Matches.Groups[1].Value }

  if ($matches.Count -eq 0) {
    throw 'No "Developer ID Application" certificate found in the keychain. Create one in Xcode: Settings > Accounts > Manage Certificates > + > Developer ID Application.'
  }
  if ($matches.Count -gt 1) {
    throw "Multiple developer certificates found: $($matches -join ', '). Pass -SigningIdentity to choose one."
  }

  return $matches[0]
}

function Build-MacOSPackage($PackageName, $RootPath) {
  if (!(Get-Command appdmg -ErrorAction Ignore)) {
    throw 'Missing appdmg. Install using "npm install -g appdmg"'
  }

  if (-not $SigningIdentity) {
    $script:SigningIdentity = Get-MacOSSigningIdentity
    Write-Host "Using signing identity: $SigningIdentity"
  }

  $dmgSpecPath = Join-Path $RootPath 'dmgspec.json'
  Copy-Item (Join-Path $PSScriptRoot 'dmgspec.json') $dmgSpecPath -Force

  $dmgPath = Join-Path $PSScriptRoot "RoslynPad-$PackageName.dmg"
  Remove-Item $dmgPath -ErrorAction Ignore
  Write-Host "Creating package $dmgPath..."
  appdmg $dmgSpecPath $dmgPath

  if ($SigningIdentity -and $NotaryProfile) {
    Notarize-MacOSDmg -DmgPath $dmgPath -Identity $SigningIdentity -Profile $NotaryProfile
  }

  # Stapling rewrites the image, so hash only once the package is final.
  $script:MacOSHashes[$PackageName] = (Get-FileHash $dmgPath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Update-HomebrewCask {
  if (!$MacOSHashes['macos-arm64'] -or !$MacOSHashes['macos-x64']) {
    return
  }

  Write-Host 'Updating Homebrew cask...'

  $caskPath = Join-Path 'brew' 'roslynpad.rb'
  $cask = Get-Content $caskPath
  $cask = $cask -replace 'version "[^"]*"', "version `"$(Get-ReleaseTag)`""
  $cask = $cask -replace '(arm:\s+)"[0-9a-f]{64}"', ('$1"' + $MacOSHashes['macos-arm64'] + '"')
  $cask = $cask -replace '(intel:\s+)"[0-9a-f]{64}"', ('$1"' + $MacOSHashes['macos-x64'] + '"')
  Set-Content -Path $caskPath -Value $cask

  # Mirror into the tap checkout when it sits next to the repo, like winget-pkgs.
  $tapCaskPath = Join-Path $PSScriptRoot '..' '..' 'homebrew-tap' 'Casks' 'roslynpad.rb'
  if (Test-Path $tapCaskPath) {
    Copy-Item $caskPath $tapCaskPath -Force
    Write-Host "Copied cask to $((Resolve-Path $tapCaskPath).Path)"
  }
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
  $releaseVersion = Get-ReleaseTag
  $hash = (Get-FileHash $archiveFile -Algorithm SHA256).Hash
  $wingetManifest = $wingetManifest -replace 'PackageVersion:.*', "PackageVersion: $version"
  $wingetManifest = $wingetManifest -replace 'InstallerUrl:.*', "InstallerUrl: https://github.com/roslynpad/roslynpad/releases/download/$releaseVersion/RoslynPad-$PackageName.zip"
  $wingetManifest = $wingetManifest -replace 'InstallerSha256:.*', "InstallerSha256: $hash"
  Set-Content -Path $wingetManifestPath -Value $wingetManifest

  if ($IsWindows) {
    ./CreateAppxPackage.ps1 -PackageName $PackageName -RootPath $RootPath
  }
}

function Build-Package($PackageName, $RuntimeIdentifier) {
  Write-Host "Building $PackageName..."

  $isWindowsPackage = $RuntimeIdentifier -like 'win-*'
  $buildPath = Join-Path '..' 'src' 'RoslynPad'
  dotnet publish $buildPath -r $RuntimeIdentifier -p:ContinuousIntegrationBuild=true ($RuntimeIdentifier -like 'osx-*' ? '-p:SignMacBundle=true' : $null)

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

Build-Package -PackageName 'macos-x64' -RuntimeIdentifier 'osx-x64'
Build-Package -PackageName 'macos-arm64' -RuntimeIdentifier 'osx-arm64'
Update-HomebrewCask
Build-Package -PackageName 'linux-x64' -RuntimeIdentifier 'linux-x64'
Build-Package -PackageName 'linux-arm64' -RuntimeIdentifier 'linux-arm64'
Build-Package -PackageName 'windows-x64' -RuntimeIdentifier 'win-x64'
Build-Package -PackageName 'windows-arm64' -RuntimeIdentifier 'win-arm64'
