$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Get-DirectoryBuildProps {
    $propsFile = Join-Path $PSScriptRoot '..' 'Directory.Build.props'
    return [xml] (Get-Content $propsFile)
}

function Get-TargetFramework {
    return ([string](Get-DirectoryBuildProps).Project.PropertyGroup.DefaultTargetFramework).Trim()
}

function Get-RoslynPadVersion($PatchVersion) {
    $versionString = ([string](Get-DirectoryBuildProps).Project.PropertyGroup.RoslynPadVersion).Trim()

    if ($PatchVersion) {
        $version = [Version] $versionString
        $version = [Version]::new($version.Major, $version.Minor, $PatchVersion)
        $versionString = $version.ToString()
    }

    return $versionString
}

function Get-AdditionalDirectory($RootPath, $Name) {
    if (Test-Path "$RootPath/$Name") {
        return Get-ChildItem (Join-Path $RootPath $Name '*.*') -Recurse -File | Select-Object -ExpandProperty FullName
    }

    return @()
}

function Get-PackageFiles($RootPath) {
    $exclude = @();

    $files = Get-ChildItem (Join-Path $rootPath '*.*') -File |
    Where-Object { $exclude -notcontains $_.Name } |
    Select-Object -ExpandProperty FullName

    $files += Get-AdditionalDirectory $RootPath 'runtimes'
    $files += Get-AdditionalDirectory $RootPath 'Themes'

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
