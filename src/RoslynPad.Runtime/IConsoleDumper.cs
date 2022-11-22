using System;
using System.IO;

namespace RoslynPad.Runtime;

internal interface IConsoleDumper
{
    bool SupportsRedirect { get; }
    TextWriter CreateWriter(string? header = null);
    TextReader CreateReader();
    void Dump(in DumpData data);
    void DumpException(Exception exception);
    void DumpProgress(ProgressResultObject result);
    void Flush();
}
