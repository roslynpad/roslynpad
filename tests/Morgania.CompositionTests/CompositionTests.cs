using System.Composition;
using System.Composition.Hosting;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.CompositionTests;

[TestClass]
public sealed class CompositionTests
{
    // Note: hosts are intentionally not shared between tests. A failed resolution
    // poisons System.Composition's export-descriptor cache for the whole container,
    // so one test's probing would corrupt another's assertions.

    [TestMethod]
    public void ContainerComposes()
    {
        using var host = EditorCatalog.CreateHost();
        Assert.IsNotNull(host);
    }

    /// <summary>
    /// Touches every export in the catalog, compensating for System.Composition's lazy
    /// failure mode: a part with a broken import only fails when first requested.
    /// An exported contract that yields zero exports means discovery silently skipped or
    /// dropped the part — also a failure.
    /// </summary>
    [TestMethod]
    public void EveryExportResolves()
    {
        using var host = EditorCatalog.CreateHost();
        var failures = new StringBuilder();
        int contracts = 0;

        foreach (var (contractType, contractName, origin) in EnumerateExportedContracts())
        {
            contracts++;
            try
            {
                int count = 0;
                foreach (object? export in host.GetExports(contractType, contractName))
                {
                    count++; // force realization; definition-holder export values may be null
                }
                if (count == 0)
                {
                    failures.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                        $"{origin}: [{contractType.FullName} / {contractName ?? "<default>"}] produced no exports");
                }
            }
            catch (Exception ex)
            {
                failures.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                    $"{origin}: [{contractType.FullName} / {contractName ?? "<default>"}] {ex.GetBaseException().Message}");
            }
        }

        Assert.IsTrue(contracts >= 70, $"Export walk saw only {contracts} contracts; catalog is incomplete.");
        Assert.AreEqual(0, failures.Length, $"Export walk failures:\n{failures}");
    }

    /// <summary>
    /// Reference-identity assertions for known-singleton services — the guard for the
    /// MEF v1→v2 lifetime default flip.
    /// </summary>
    [TestMethod]
    public void SingletonServicesAreShared()
    {
        using var host = EditorCatalog.CreateHost();
        AssertSingleton<IContentTypeRegistryService>(host);
        AssertSingleton<IClassificationTypeRegistryService>(host);
        AssertSingleton<ITextBufferFactoryService>(host);
        AssertSingleton<IEditorOptionsFactoryService>(host);
        AssertSingleton<IGuardedOperations>(host);
        AssertSingleton<IFeatureServiceFactory>(host);
        AssertSingleton<ITextSearchService2>(host);
        AssertSingleton<ITextDifferencingSelectorService>(host);
        AssertSingleton<IBufferTagAggregatorFactoryService>(host);

        // Multi-contract parts must resolve to one instance across all their contracts.
        Assert.AreSame(
            (object)host.GetExport<ITextBufferFactoryService>(),
            host.GetExport<IProjectionBufferFactoryService>(),
            "BufferFactoryService must be one shared part across its export contracts.");
    }

    /// <summary>
    /// Functional probe of the content-type registry: exercises property exports of
    /// ContentTypeDefinition and the converted metadata views end to end.
    /// </summary>
    [TestMethod]
    public void ContentTypeRegistryResolvesStandardContentTypes()
    {
        using var host = EditorCatalog.CreateHost();
        var registry = host.GetExport<IContentTypeRegistryService>();

        var text = registry.GetContentType("text");
        Assert.IsNotNull(text);
        Assert.IsTrue(text.BaseTypes.Any(static b => b.TypeName == "any"), "'text' must derive from 'any'.");
        Assert.IsNotNull(registry.GetContentType("code"));
        Assert.IsNotNull(registry.GetContentType("projection"));
    }

    private static void AssertSingleton<T>(CompositionHost host) where T : class
    {
        T first = host.GetExport<T>();
        T second = host.GetExport<T>();
        Assert.AreSame(first, second, $"{typeof(T).Name} must be a shared singleton.");
    }

    private static IEnumerable<(Type ContractType, string? ContractName, string Origin)> EnumerateExportedContracts()
    {
        var seen = new HashSet<(Type, string?)>();
        foreach (Assembly assembly in EditorCatalog.LoadAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (var attribute in type.GetCustomAttributes<ExportAttribute>(inherit: false))
                {
                    var contract = (attribute.ContractType ?? type, attribute.ContractName);
                    if (seen.Add(contract))
                    {
                        yield return (contract.Item1, contract.Item2, type.FullName!);
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    foreach (var attribute in property.GetCustomAttributes<ExportAttribute>(inherit: false))
                    {
                        var contract = (attribute.ContractType ?? property.PropertyType, attribute.ContractName);
                        if (seen.Add(contract))
                        {
                            yield return (contract.Item1, contract.Item2, $"{type.FullName}.{property.Name}");
                        }
                    }
                }
            }
        }
    }
}
