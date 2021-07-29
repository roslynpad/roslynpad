
$ErrorActionPreference = 'Stop'; # stop on all errors
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://github.com/aelij/RoslynPad/releases/download/15.1/RoslynPad.zip' 
$packageName = "RoslynPad"

"$PSScriptRoot\..\src\RoslynPad"

# To determine checksums, you can get that from the original site if provided. 
# You can also use checksum.exe (choco install checksum) and use it 
# e.g. checksum -t sha256 -f path\to\file
$checksum      = '8739C76E681F900923B900C9DF0EF75CF421D39CABB54650C4B9AD19B6A76D85'
$checksumType  = 'sha256' #default is md5, can also be sha1, sha256 or sha512

Install-ChocolateyZipPackage -PackageName $packageName -Url $url -UnzipLocation $toolsDir -checksum $checksum -checksumType $checksumType
