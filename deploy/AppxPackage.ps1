$env:Path += ";C:\Program Files (x86)\Windows Kits\10\bin\x64"

$mapping = "RoslynPad.mapping"
Remove-Item $mapping -ErrorAction Ignore

$location = Get-Location

. .\GetFiles.ps1

"[Files]" >> $mapping
('"' + $location + '\AppxManifest.xml" "AppxManifest.xml"') >> $mapping
foreach ($asset in Get-ChildItem Assets)
{
	('"' + $asset.FullName + '" "Assets\' + $asset.Name + '"') >> $mapping
}
foreach ($file in $files)
{
	$target = "$location\$binPath\$file"
	('"' + $target + '" "' + $file + '"') >> $mapping
	$file
}

MakeAppx.exe pack /f $mapping /l /p RoslynPad.appx
