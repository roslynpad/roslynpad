PARAM (
	[Switch]
	$Avalonia
)

$ErrorActionPreference = 'Stop'

Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

$location = $PSScriptRoot

if ($Avalonia) {
	dotnet build "$PSScriptRoot\..\src\RoslynPad.Avalonia" -c Release
	. .\GetFiles.ps1 -Avalonia
	$archiveFile = "$location\RoslynPadAvalonia.zip"
}
else {
	dotnet build "$PSScriptRoot\..\src\RoslynPad" -c Release
	. .\GetFiles.ps1
	$archiveFile = "$location\RoslynPad.zip"
}

Remove-Item $archiveFile -ErrorAction Ignore

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
