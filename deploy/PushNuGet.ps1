$ErrorActionPreference = 'Stop'

dotnet pack $PSScriptRoot\..\src -c Release
Remove-Item -recurse $PSScriptRoot\..\src\*.nupkg
Get-ChildItem -recurse $PSScriptRoot\..\src\*.nupkg | `
    ForEach-Object { dotnet nuget push $_.FullName --source nuget.org }
