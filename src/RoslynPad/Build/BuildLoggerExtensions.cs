using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace RoslynPad.Build;

internal static partial class BuildLoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Start ExecuteAsync")]
    public static partial void StartExecuteAsync(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Assembly saved at {AssemblyPath}, has errors = {HasErrors}")]
    public static partial void AssemblySaved(this ILogger logger, string? assemblyPath, bool hasErrors);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Creating script runner, platform = {Platform}, references = {References}, imports = {Imports}, directory = {Directory}, optimization = {Optimization}")]
    public static partial void CreatingScriptRunner(
        this ILogger logger,
        Microsoft.CodeAnalysis.Platform platform,
        IEnumerable<string?> references,
        IEnumerable<string> imports,
        string directory,
        OptimizationLevel? optimization);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Error killing process")]
    public static partial void ErrorKillingProcess(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Starting process {Executable}, arguments = {Arguments}")]
    public static partial void StartingProcess(this ILogger logger, string executable, string? arguments);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Process.Start returned false")]
    public static partial void ProcessStartReturnedFalse(this ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Error in previous restore task")]
    public static partial void ErrorInPreviousRestoreTask(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Restore error")]
    public static partial void RestoreError(this ILogger logger, Exception exception);
}
