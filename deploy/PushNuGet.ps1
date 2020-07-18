ls -recurse ..\src\*.nupkg | % { dotnet nuget push $_.FullName --source nuget.org }
rm -recurse ..\src\*.nupkg
