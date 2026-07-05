using System.Collections.Frozen;
using System.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RoslynPad.Editor;

/// <summary>
/// The content type for disassembled IL shown in the IL viewer.
/// </summary>
public sealed class ILClassificationDefinitions
{
    public const string ContentType = "ILAsm";

    [Export]
    [Name(ContentType)]
    [BaseDefinition("text")]
    public ContentTypeDefinition? ILContentType { get; }
}

/// <summary>
/// Classifies disassembled IL over Roslyn's classification types, so the IL viewer picks up
/// the same theme colors as the code editor.
/// </summary>
[Export(typeof(IClassifierProvider))]
[ContentType(ILClassificationDefinitions.ContentType)]
public sealed class ILClassifierProvider : IClassifierProvider
{
    private readonly IClassificationTypeRegistryService _classificationTypes;

    [ImportingConstructor]
    public ILClassifierProvider(IClassificationTypeRegistryService classificationTypes)
    {
        _classificationTypes = classificationTypes;
    }

    public IClassifier GetClassifier(ITextBuffer textBuffer)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        return textBuffer.Properties.GetOrCreateSingletonProperty(() => new ILClassifier(_classificationTypes));
    }

    private sealed class ILClassifier(IClassificationTypeRegistryService registry) : IClassifier
    {
        private readonly IClassificationType _keyword = registry.GetClassificationType(ClassificationLayer.Semantic, "keyword");
        private readonly IClassificationType _directive = registry.GetClassificationType(ClassificationLayer.Semantic, "preprocessor keyword");
        private readonly IClassificationType _comment = registry.GetClassificationType(ClassificationLayer.Semantic, "comment");
        private readonly IClassificationType _string = registry.GetClassificationType(ClassificationLayer.Semantic, "string");

#pragma warning disable CS0067 // The classification of a span never changes after load.
        public event EventHandler<ClassificationChangedEventArgs>? ClassificationChanged;
#pragma warning restore CS0067

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            var snapshot = span.Snapshot;
            int firstLine = snapshot.GetLineNumberFromPosition(span.Start);
            int lastLine = snapshot.GetLineNumberFromPosition(span.End);
            for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
            {
                ClassifyLine(snapshot.GetLineFromLineNumber(lineNumber), result);
            }

            return result;
        }

        private void ClassifyLine(ITextSnapshotLine line, List<ClassificationSpan> result)
        {
            string text = line.GetText();
            int lineStart = line.Start.Position;

            void Add(int start, int end, IClassificationType type) =>
                result.Add(new ClassificationSpan(new SnapshotSpan(line.Snapshot, lineStart + start, end - start), type));

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    Add(i, text.Length, _comment);
                    return;
                }

                if (c == '"')
                {
                    int end = text.IndexOf('"', i + 1);
                    end = end < 0 ? text.Length : end + 1;
                    Add(i, end, _string);
                    i = end - 1;
                    continue;
                }

                if (IsWordChar(c))
                {
                    int start = i;
                    while (i + 1 < text.Length && IsWordChar(text[i + 1]))
                    {
                        i++;
                    }

                    var word = text.AsSpan(start, i - start + 1);
                    if (s_keywordLookup.Contains(word))
                    {
                        Add(start, i + 1, _keyword);
                    }
                    else if (s_directiveLookup.Contains(word))
                    {
                        Add(start, i + 1, _directive);
                    }
                }
            }
        }

        private static bool IsWordChar(char c) =>
            char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '#';

        private static readonly FrozenSet<string> s_keywords = new[]
        {
            // instructions
            "nop", "break", "ldarg.0", "ldarg.1", "ldarg.2", "ldarg.3", "ldloc.0", "ldloc.1", "ldloc.2", "ldloc.3",
            "stloc.0", "stloc.1", "stloc.2", "stloc.3", "ldarg.s", "ldarga.s", "starg.s", "ldloc.s", "ldloca.s",
            "stloc.s", "ldnull", "ldc.i4.m1", "ldc.i4.0", "ldc.i4.1", "ldc.i4.2", "ldc.i4.3", "ldc.i4.4", "ldc.i4.5",
            "ldc.i4.6", "ldc.i4.7", "ldc.i4.8", "ldc.i4.s", "ldc.i4", "ldc.i8", "ldc.r4", "ldc.r8", "dup", "pop",
            "jmp", "call", "calli", "ret", "br.s", "brfalse.s", "brtrue.s", "beq.s", "bge.s", "bgt.s", "ble.s",
            "blt.s", "bne.un.s", "bge.un.s", "bgt.un.s", "ble.un.s", "blt.un.s", "br", "brfalse", "brtrue", "beq",
            "bge", "bgt", "ble", "blt", "bne.un", "bge.un", "bgt.un", "ble.un", "blt.un", "switch", "ldind.i1",
            "ldind.u1", "ldind.i2", "ldind.u2", "ldind.i4", "ldind.u4", "ldind.i8", "ldind.i", "ldind.r4",
            "ldind.r8", "ldind.ref", "stind.ref", "stind.i1", "stind.i2", "stind.i4", "stind.i8", "stind.r4",
            "stind.r8", "add", "sub", "mul", "div", "div.un", "rem", "rem.un", "and", "or", "xor", "shl", "shr",
            "shr.un", "neg", "not", "conv.i1", "conv.i2", "conv.i4", "conv.i8", "conv.r4", "conv.r8", "conv.u4",
            "conv.u8", "callvirt", "cpobj", "ldobj", "ldstr", "newobj", "castclass", "isinst", "conv.r.un", "unbox",
            "throw", "ldfld", "ldflda", "stfld", "ldsfld", "ldsflda", "stsfld", "stobj", "conv.ovf.i1.un",
            "conv.ovf.i2.un", "conv.ovf.i4.un", "conv.ovf.i8.un", "conv.ovf.u1.un", "conv.ovf.u2.un",
            "conv.ovf.u4.un", "conv.ovf.u8.un", "conv.ovf.i.un", "conv.ovf.u.un", "box", "newarr", "ldlen",
            "ldelema", "ldelem.i1", "ldelem.u1", "ldelem.i2", "ldelem.u2", "ldelem.i4", "ldelem.u4", "ldelem.i8",
            "ldelem.i", "ldelem.r4", "ldelem.r8", "ldelem.ref", "stelem.i", "stelem.i1", "stelem.i2", "stelem.i4",
            "stelem.i8", "stelem.r4", "stelem.r8", "stelem.ref", "conv.ovf.i1", "conv.ovf.u1", "conv.ovf.i2",
            "conv.ovf.u2", "conv.ovf.i4", "conv.ovf.u4", "conv.ovf.i8", "conv.ovf.u8", "refanyval", "ckfinite",
            "mkrefany", "ldtoken", "conv.u2", "conv.u1", "conv.i", "conv.ovf.i", "conv.ovf.u", "add.ovf",
            "add.ovf.un", "mul.ovf", "mul.ovf.un", "sub.ovf", "sub.ovf.un", "endfinally", "leave", "leave.s",
            "stind.i", "conv.u", "prefix7", "prefix6", "prefix5", "prefix4", "prefix3", "prefix2", "prefix1",
            "prefixref", "arglist", "ceq", "cgt", "cgt.un", "clt", "clt.un", "ldftn", "ldvirtftn", "ldarg", "ldarga",
            "starg", "ldloc", "ldloca", "stloc", "localloc", "endfilter", "unaligned.", "volatile.", "tail.",
            "initobj", "cpblk", "initblk", "rethrow", "sizeof", "refanytype", "illegal", "endmac", "brnull",
            "brnull.s", "brzero", "brzero.s", "brinst", "brinst.s", "ldind.u8", "ldelem.u8", "ldc.i4.M1", "endfault",
            // keywords
            "void", "bool", "char", "wchar", "int", "int8", "int16", "int32", "int64", "float", "float32",
            "float64", "refany", "typedref", "object", "string", "native", "unsigned", "value", "valuetype",
            "class", "const", "vararg", "default", "stdcall", "thiscall", "fastcall", "unmanaged", "not_in_gc_heap",
            "beforefieldinit", "instance", "filter", "catch", "static", "public", "private", "synchronized",
            "interface", "extends", "implements", "handler", "finally", "fault", "to", "abstract", "auto",
            "sequential", "explicit", "wrapper", "ansi", "unicode", "autochar", "import", "enum", "virtual",
            "notremotable", "special", "il", "cil", "optil", "managed", "preservesig", "runtime", "method", "field",
            "bytearray", "final", "sealed", "specialname", "family", "assembly", "famandassem", "famorassem",
            "privatescope", "nested", "hidebysig", "newslot", "rtspecialname", "pinvokeimpl", "unmanagedexp",
            "reqsecobj", ".ctor", ".cctor", "initonly", "literal", "notserialized", "forwardref", "internalcall",
            "noinlining", "nomangle", "lasterr", "winapi", "cdecl", "as", "pinned", "modreq", "modopt",
            "serializable", "at", "tls", "true", "false", "strict",
            // security actions
            "request", "demand", "assert", "deny", "permitonly", "linkcheck", "inheritcheck", "reqmin", "reqopt",
            "reqrefuse", "prejitgrant", "prejitdeny", "noncasdemand", "noncaslinkdemand", "noncasinheritance",
        }.ToFrozenSet(StringComparer.Ordinal);

        private static readonly FrozenSet<string> s_directives = new[]
        {
            ".class", ".namespace", ".method", ".field", ".emitbyte", ".try", ".maxstack", ".locals", ".entrypoint",
            ".zeroinit", ".pdirect", ".data", ".event", ".addon", ".removeon", ".fire", ".other", "protected",
            ".property", ".set", ".get", ".import", ".permission", ".permissionset", ".line", ".language", "#line",
            ".custom", "init", ".size", ".pack", ".file", "nometadata", ".hash", ".assembly", "implicitcom",
            "noappdomain", "noprocess", "nomachine", ".publickey", ".publickeytoken", "algorithm", ".ver",
            ".locale", "extern", ".export", ".manifestres", ".mresource", ".localized", ".module", "marshal",
            "custom", "sysstring", "fixed", "variant", "currency", "syschar", "decimal", "date", "bstr", "tbstr",
            "lpstr", "lpwstr", "lptstr", "objectref", "iunknown", "idispatch", "struct", "safearray", "byvalstr",
            "lpvoid", "any", "array", "lpstruct", ".vtfixup", "fromunmanaged", "callmostderived", ".vtentry", "in",
            "out", "opt", "lcid", "retval", ".param", ".override", "with", "null", "error", "hresult", "carray",
            "userdefined", "record", "filetime", "blob", "stream", "storage", "streamed_object", "stored_object",
            "blob_object", "cf", "clsid", "vector", "nullref", ".subsystem", ".corflags", "alignment", ".imagebase",
        }.ToFrozenSet(StringComparer.Ordinal);

        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> s_keywordLookup =
            s_keywords.GetAlternateLookup<ReadOnlySpan<char>>();

        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> s_directiveLookup =
            s_directives.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
