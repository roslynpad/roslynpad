ls -recurse ..\src\*.nupkg | % { nuget push $_.FullName -source nuget.org }
rm -recurse ..\src\*.nupkg
