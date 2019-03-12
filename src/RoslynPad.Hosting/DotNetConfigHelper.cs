using Mono.Cecil;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RoslynPad.Hosting
{
    internal static class DotNetConfigHelper
    {
        private static readonly XNamespace AsmNs = "urn:schemas-microsoft-com:asm.v1";

        public static JObject CreateNetCoreRuntimeOptions(PlatformVersion version)
        {
            return new JObject(
                new JProperty("runtimeOptions", new JObject(
                    new JProperty("tfm", version.TargetFrameworkMoniker),
                    new JProperty("framework", new JObject(
                        new JProperty("name", version.FrameworkName),
                        new JProperty("version", version.FrameworkVersion))))));
        }

        public static JObject CreateNetCoreDevRuntimeOptions(string packageFolder)
        {
            return new JObject(
                new JProperty("runtimeOptions", new JObject(
                    new JProperty("additionalProbingPaths", new JArray(packageFolder)))));
        }

        public static XDocument CreateNetFxAppConfig(IList<string> references)
        {
            var runtime = new XElement("runtime");

            foreach (var file in references)
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(file))
                {
                    var publicKeyToken = assembly.Name.PublicKeyToken;
                    var publicKeyTokenString = publicKeyToken == null || publicKeyToken.Length == 0
                        ? string.Empty
                        : string.Join("", publicKeyToken.Select(t => t.ToString("x2")));

                    var element = new XElement(AsmNs + "assemblyBinding",
                        new XElement(AsmNs + "dependentAssembly",
                            new XElement(AsmNs + "assemblyIdentity",
                                new XAttribute("name", assembly.Name.Name),
                                new XAttribute("publicKeyToken", publicKeyTokenString),
                                new XAttribute("culture", string.IsNullOrEmpty(assembly.Name.Culture) ? "neutral" : assembly.Name.Culture)),
                            new XElement(AsmNs + "bindingRedirect",
                                new XAttribute("oldVersion", "0.0.0.0-" + assembly.Name.Version),
                                new XAttribute("newVersion", assembly.Name.Version))));

                    runtime.Add(element);
                }
            }

            return new XDocument(
                new XElement("configuration",
                    new XElement(runtime)));
        }
    }
}
