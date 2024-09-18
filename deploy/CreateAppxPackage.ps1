param (
  [string] $RootPath,
  [string] $PackageName,
  [string] $PatchVersion = '0'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true


Set-Location $PSScriptRoot

. .\Common.ps1

$toolsPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\"
$toolsPath += "\$(Get-LatestVersionPath $toolsPath)\x64"
$env:Path += ";$toolsPath"

$packageFile = "RoslynPad-$PackageName.appx"
$mapping = 'RoslynPad.mapping'
Remove-Item $mapping -ErrorAction Ignore
Remove-Item $packageFile -ErrorAction Ignore
Remove-Item *.pri

Write-Host 'Updating manifest...'

$appManifestPath = "$PSScriptRoot\resources\windows\PackageRoot\AppxManifest.xml"
$appManifest = [xml] (Get-Content $appManifestPath)
$appManifest.Package.Identity.Version = (Get-RoslynPadVersion $PatchVersion) + '.0'
$appManifest.Save($appManifestPath)

$files = Get-PackageFiles $RootPath

Write-Host 'Creating mapping...'

"[Files]" >> $mapping

('"' + $appManifestPath + '" "AppxManifest.xml"') >> $mapping

foreach ($asset in Get-ChildItem resources\windows\PackageRoot\Assets) {
  ('"' + $asset.FullName + '" "Assets\' + $asset.Name + '"') >> $mapping
}

foreach ($file in $files) {
  ('"' + $file + '" "' + $file.Substring($RootPath.Length) + '"') >> $mapping
  $file
}

Write-Host 'Creating PRI...'

makepri.exe new /pr resources\windows\PackageRoot /cf priconfig.xml

foreach ($file in Get-ChildItem *.pri) {
  ('"' + $file + '" "' + $file.BaseName + '.pri"') >> $mapping
  $file | Write-Host
}

Write-Host 'Creating APPX...'

MakeAppx.exe pack /f $mapping /l /p $packageFile

Write-Host 'Signing package...'

if (!(Test-Path .\RoslynPad.pfx)) {
  $cert = New-SelfSignedCertificate -Type Custom `
    -Subject "CN=9C7E53B6-ADB4-497A-97E5-B4B9B239B179" `
    -KeyUsage DigitalSignature `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

  Export-PfxCertificate -Cert $cert -FilePath RoslynPad.pfx `
    -Password (ConvertTo-SecureString -String 'a' -Force -AsPlainText)
}

signtool.exe sign -f RoslynPad.pfx -p 'a' -fd SHA256 -v $packageFile
