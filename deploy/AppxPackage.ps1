$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Set-Location $PSScriptRoot

. .\Common.ps1

$toolsPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Shared\NuGetPackages\microsoft.windows.sdk.buildtools"
$toolsPath += "\$(Get-LatestVersionPath $toolsPath)\bin"
$toolsPath += "\$(Get-LatestVersionPath $toolsPath)\x64"
$env:Path += ";$toolsPath"

$mapping = 'RoslynPad.mapping'
Remove-Item $mapping -ErrorAction Ignore
Remove-Item RoslynPad.appx -ErrorAction Ignore
Remove-Item *.pri

Write-Host 'Updating manifest...'

$appManifestPath = "$PSScriptRoot\PackageRoot\AppxManifest.xml"
$appManifest = [xml] (Get-Content $appManifestPath)
$appManifest.Package.Identity.Version = (Get-RoslynPadVersion) + '.0'
$appManifest.Save($appManifestPath)

Write-Host 'Building...'

dotnet publish .\..\src\RoslynPad -c Release --self-contained -r win-x64

$rootPath = Get-PackageRoot -Published
$files = Get-PackageFiles $rootPath

Write-Host 'Creating mapping...'

"[Files]" >> $mapping

('"' + $appManifestPath + '" "AppxManifest.xml"') >> $mapping

foreach ($asset in Get-ChildItem PackageRoot\Assets) {
  ('"' + $asset.FullName + '" "Assets\' + $asset.Name + '"') >> $mapping
}

foreach ($file in $files) {
  ('"' + $file + '" "' + $file.Substring($rootPath.Length) + '"') >> $mapping
  $file
}

Write-Host 'Creating PRI...'

makepri.exe new /pr PackageRoot /cf priconfig.xml

foreach ($file in Get-ChildItem *.pri) {
  ('"' + $file + '" "' + $file.BaseName + '.pri"') >> $mapping
  $file
}

Write-Host 'Creating APPX...'

MakeAppx.exe pack /f $mapping /l /p RoslynPad.appx

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

signtool.exe sign -f RoslynPad.pfx -p 'a' -fd SHA256 -v RoslynPad.appx
