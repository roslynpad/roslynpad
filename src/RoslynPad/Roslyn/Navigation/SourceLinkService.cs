using System.Composition;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.PdbSourceDocument;

namespace RoslynPad.Roslyn.Navigation;

/// <summary>
/// Downloads portable PDBs from the public symbol servers and Source Link source files over
/// HTTP — the piece Roslyn otherwise delegates to the VS/VS Code debugger. With it present,
/// navigating to symbols built with Source Link (the .NET runtime, most NuGet packages) opens
/// the original sources instead of falling back to decompilation.
/// </summary>
[Export(typeof(ISourceLinkService)), Shared]
internal sealed class SourceLinkService : ISourceLinkService
{
    private static readonly HttpClient s_httpClient = new();
    private static readonly string s_cacheRoot = Path.Combine(Path.GetTempPath(), "roslynpad", "symbols");
    private static readonly string[] s_symbolServers =
    [
        "https://msdl.microsoft.com/download/symbols",
        "https://symbols.nuget.org/download/symbols",
    ];

    public async Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, bool useDefaultSymbolServers, CancellationToken cancellationToken)
    {
        foreach (var entry in peReader.ReadDebugDirectory())
        {
            if (entry.Type != DebugDirectoryEntryType.CodeView || !entry.IsPortableCodeView)
            {
                continue;
            }

            var codeView = peReader.ReadCodeViewDebugDirectoryData(entry);
            var pdbName = Path.GetFileName(codeView.Path);
            // SSQP portable PDB key: {name}/{signature guid}FFFFFFFF/{name}, lowercase.
            var signature = $"{codeView.Guid:N}ffffffff";
            var cachePath = Path.Combine(s_cacheRoot, "pdb", signature, pdbName);
            if (File.Exists(cachePath))
            {
                return new PdbFilePathResult(cachePath);
            }

            var escapedName = Uri.EscapeDataString(pdbName.ToLowerInvariant());
            foreach (var server in s_symbolServers)
            {
                if (await TryDownloadAsync($"{server}/{escapedName}/{signature}/{escapedName}", cachePath, cancellationToken).ConfigureAwait(false))
                {
                    return new PdbFilePathResult(cachePath);
                }
            }
        }

        return null;
    }

    public async Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, CancellationToken cancellationToken)
    {
        var urlHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];
        var cachePath = Path.Combine(s_cacheRoot, "src", urlHash, Path.GetFileName(relativePath));
        return File.Exists(cachePath) || await TryDownloadAsync(url, cachePath, cancellationToken).ConfigureAwait(false)
            ? new SourceFilePathResult(cachePath)
            : null;
    }

    private static async Task<bool> TryDownloadAsync(string url, string targetPath, CancellationToken cancellationToken)
    {
        // One retry: a transient failure here silently downgrades navigation to decompilation
        // for the rest of the session (the generated file is cached per symbol).
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var response = await s_httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                var tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                var file = File.Create(tempPath);
                await using (file.ConfigureAwait(false))
                {
                    await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, targetPath, overwrite: true);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }
        }

        return false;
    }
}
