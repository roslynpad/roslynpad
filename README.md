# RoslynPad

![RoslynPad](src/RoslynPad/Resources/RoslynPad.png)

A cross-platform C# editor based on Roslyn and AvalonEdit

[![Downloads](https://img.shields.io/github/downloads/aelij/RoslynPad/total.svg?style=flat-square)](https://github.com/aelij/RoslynPad/releases)

Also available to download in the Microsoft Store:

<a href="https://www.microsoft.com/store/apps/9nctj2cqwxv0?ocid=badge"><img src="https://assets.windowsphone.com/f2f77ec7-9ba9-4850-9ebe-77e366d08adc/English_Get_it_Win_10_InvariantCulture_Default.png" width="200" alt="Get it on Windows 10" /></a>

## Building

Open `src\RoslynPad.sln` in Visual Studio 2017.

## Try out RoslynPad for Mac/Linux (alpha)

* Install .NET Core SDK 1.1
* Review the Avalonia [platform prerequisites](https://github.com/AvaloniaUI/Avalonia/wiki/Platform-support) (mainly requires GTK3)
  * On a Mac, just use [brew](https://brew.sh/): `brew install gtk+3`
* Run these commands:

  ```
  git clone https://github.com/aelij/roslynpad
  cd roslynpad/src/RoslynPad.NetCore
  dotnet restore
  dotnet run
  ```

## Features

### Completion

![Completion](docs/Completion.png)

### Signature Help

![Signature Help](docs/SignatureHelp.png)

### Diagnostics

![Diagnostics](docs/Diagnostics.png)

### Code Fixes

![Code Fixes](docs/CodeFixes.png)
