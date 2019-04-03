PARAM
(
    [Switch]
    $NetCore
)
Add-Type -A 'System.IO.Compression'
Add-Type -A 'System.IO.Compression.FileSystem'

$location = $PSScriptRoot

if ($NetCore)
{
    . .\GetFilesNetCore.ps1
    $archiveFile = "$location\RoslynPadNetCore.zip"
}
else
{
    . .\GetFiles.ps1
    $archiveFile = "$location\RoslynPad.zip"
}

Remove-Item $archiveFile -ErrorAction Ignore

try
{
	$archive = [System.IO.Compression.ZipFile]::Open($archiveFile, [System.IO.Compression.ZipArchiveMode]::Create)

	foreach ($file in $files)
	{
		[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $file.Substring($rootPath.Length).Replace("\", "/")) | Out-Null
		$file
	}
}
finally
{
	$archive.Dispose()
}
