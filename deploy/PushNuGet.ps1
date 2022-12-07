$ErrorActionPreference = 'Stop'

Remove-Item -Recurse $PSScriptRoot\..\src\*.nupkg

dotnet pack $PSScriptRoot\..\src -c Release

Get-ChildItem -Recurse $PSScriptRoot\..\src\*.nupkg | `
    ForEach-Object { dotnet nuget push $_.FullName --source nuget.org }
