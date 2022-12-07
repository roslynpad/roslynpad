$ErrorActionPreference = 'Stop'

function Get-DirectoryBuildProps {
  $propsFile = Join-Path $PSScriptRoot '..' '.\Directory.Build.props'
  return [xml] (Get-Content $propsFile)
}

function Get-TargetFramework {
  return (Get-DirectoryBuildProps).Project.PropertyGroup.DefaultTargetFramework
}

function Get-RoslynPadVersion {
  return (Get-DirectoryBuildProps).Project.PropertyGroup.RoslynPadVersion
}

function Get-PackageRoot {
  param (
    [switch] $Avalonia,
    [switch] $Published
  )

  $project = if ($Avalonia) { 'RoslynPad.Avalonia' } else { 'RoslynPad' }
  $targetFramework = if ($Avalonia) { Get-TargetFramework } else { "$(Get-TargetFramework)-windows" }
  $path = if ($Published) { "bin\Release\$targetFramework\win-x64\publish" } else { "bin\Release\$targetFramework" }
  return (Resolve-Path "$PSScriptRoot\..\src\$project\$path").Path
}

function Get-PackageFiles($RootPath) {
  $exclude = @();

  $files = Get-ChildItem "$rootPath\*.*" -file |
    Where-Object { $exclude -notcontains $_.Name } |
    Select-Object -ExpandProperty FullName

  if (Test-Path "$rootPath\runtimes") {
    $files += Get-ChildItem "$rootPath\runtimes\*.*" -Recurse -File | Select-Object -ExpandProperty FullName
  }

  return $files
}

function Get-LatestVersionPath($Path) {
  $version = Get-ChildItem -Directory -Path $Path | 
    ForEach-Object { $version = $null; [Version]::TryParse($_.Name, [ref] $version) | Out-Null; $version } |
    Sort-Object -Descending | Select-Object -First 1
  
  if (!$version) {
    throw "Unable to find a versioned directory under $Path"
  }

  return $version
}
