$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Remove-Item -Recurse $PSScriptRoot\..\src\*.nupkg

dotnet pack $PSScriptRoot\..\RoslynPad.sln -c Release -p:EnableWindowsTargeting=true -p:ContinuousIntegrationBuild=true

$apiKey = Read-Host -Prompt "Enter nuget.org API key"
Get-ChildItem -Recurse $PSScriptRoot\..\src\*.nupkg | `
    ForEach-Object { dotnet nuget push $_.FullName --source nuget.org --api-key $apiKey }
