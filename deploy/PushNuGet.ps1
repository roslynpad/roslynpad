$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

Remove-Item -Recurse $PSScriptRoot\..\src\*.nupkg

dotnet pack $PSScriptRoot\..\RoslynPad.sln -c Release -p:ContinuousIntegrationBuild=true

Get-ChildItem -Recurse $PSScriptRoot\..\src\*.nupkg | `
    ForEach-Object { dotnet nuget push $_.FullName --source nuget.org }
