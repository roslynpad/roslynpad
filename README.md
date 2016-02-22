# RoslynPad

![RoslynPad](src/RoslynPad/Resources/RoslynPad.png)

A simple C# editor based on Roslyn and AvalonEdit

## Notes

* This project uses internal members for completion (`Microsoft.CodeAnalysis.Completion.ICompletionService`) and method signature help (`Microsoft.CodeAnalysis.Editor.ISignatureHelpProvider`) - aka "Intellisense" - via *Reflection* (instead of the previous private build of Roslyn)
* It's intended only for play and for learning how to use some Roslyn APIs
