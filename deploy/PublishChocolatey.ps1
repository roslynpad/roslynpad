$ErrorActionPreference = 'Stop'

$location = Get-Location

$appxManifest = "$location\PackageRoot\AppxManifest.xml"
$xml = [xml](get-content $appxManifest)
$version = $xml.GetElementsByTagName('Identity').GetAttribute('Version').TrimEnd('.0.0')

$checksum = checksum -t sha256 -f "$PSScriptRoot\RoslynPad.zip"

Push-Location "$location\Chocolatey\"

$chocoInstallScript = ".\tools\chocolateyinstall.ps1"
$chocoInstallContent = Get-Content $chocoInstallScript -Encoding utf8
$chocoInstallContent = $chocoInstallContent -replace 'checksum      = .*', "checksum      = '$checksum'"
Set-Content -Path $chocoInstallScript -value $chocoInstallContent -Encoding utf8NoBOM

$chocoNuspec = ".\roslynpad.nuspec"
$chocoNuspecContent = Get-Content $chocoNuspec -Encoding utf8
$chocoNuspecContent = $chocoNuspecContent -replace "<version>.*<\/version>", "<version>$version</version>"
Set-Content -Path $chocoNuspec -value $chocoNuspecContent -Encoding utf8NoBOM

$packageName = "RoslynPad.$($version).nupkg"

choco pack
choco push $packageName --source https://push.chocolatey.org/ --what-if --debug

Pop-Location