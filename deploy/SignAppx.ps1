$env:Path += ";C:\Program Files (x86)\Windows Kits\10\bin\x64"
if (!(Test-Path .\RoslynPad.pfx))
{
	MakeCert.exe -r -h 0 -n "CN=9C7E53B6-ADB4-497A-97E5-B4B9B239B179" -eku 1.3.6.1.5.5.7.3.3 -pe -sv RoslynPad.pvk RoslynPad.cer
	pvk2pfx.exe -pvk RoslynPad.pvk -spc RoslynPad.cer -pfx RoslynPad.pfx
}
signtool.exe sign -f RoslynPad.pfx -fd SHA256 -v .\RoslynPad.appx
