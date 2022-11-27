using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace RoslynPad.Roslyn.WorkspaceServices;

[ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
internal class LegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
{
    private readonly IGlobalOptionService _globalOptions;
    private readonly CodeActionOptionsStorage.Provider _provider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyGlobalOptionsWorkspaceService(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
        _provider = _globalOptions.CreateProvider();
    }

    public bool RazorUseTabs { get; }
    public int RazorTabSize { get; }
    public bool GenerateOverrides { get; set; }
    public bool InlineHintsOptionsDisplayAllOverride { get; set; }
    public CleanCodeGenerationOptionsProvider CleanCodeGenerationOptionsProvider => _provider;

    public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language) => false;
    public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language) => false;
    public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language) => false;
    public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value) { }
    public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value) { }
    public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value) { }
}
