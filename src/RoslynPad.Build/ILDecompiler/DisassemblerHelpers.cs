// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Globalization;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RoslynPad.Build.ILDecompiler;

internal enum ILNameSyntax
{
    /// <summary>
    /// class/valuetype + TypeName (built-in types use keyword syntax)
    /// </summary>
    Signature,
    /// <summary>
    /// Like signature, but always refers to type parameters using their position
    /// </summary>
    SignatureNoNamedTypeParameters,
    /// <summary>
    /// [assembly]Full.Type.Name (even for built-in types)
    /// </summary>
    TypeName,
    /// <summary>
    /// Name (but built-in types use keyword syntax)
    /// </summary>
    ShortTypeName
}

internal static class DisassemblerHelpers
{
    public static void WriteOffsetReference(ITextOutput writer, Instruction instruction)
    {
        writer.WriteReference(CecilExtensions.OffsetToString(instruction.Offset), instruction);
    }

    public static void WriteTo(this ExceptionHandler exceptionHandler, ITextOutput writer)
    {
        writer.Write("Try ");
        WriteOffsetReference(writer, exceptionHandler.TryStart);
        writer.Write('-');
        WriteOffsetReference(writer, exceptionHandler.TryEnd);
        writer.Write(' ');
        writer.Write(exceptionHandler.HandlerType.ToString());
        if (exceptionHandler.FilterStart != null)
        {
            writer.Write(' ');
            WriteOffsetReference(writer, exceptionHandler.FilterStart);
            writer.Write(" handler ");
        }
        if (exceptionHandler.CatchType != null)
        {
            writer.Write(' ');
            exceptionHandler.CatchType.WriteTo(writer);
        }
        writer.Write(' ');
        WriteOffsetReference(writer, exceptionHandler.HandlerStart);
        writer.Write('-');
        WriteOffsetReference(writer, exceptionHandler.HandlerEnd);
    }

    public static void WriteTo(this Instruction instruction, ITextOutput writer)
    {
        writer.WriteDefinition(CecilExtensions.OffsetToString(instruction.Offset), instruction);
        writer.Write(": ");
        writer.WriteReference(instruction.OpCode.Name, instruction.OpCode);
        if (instruction.Operand != null)
        {
            writer.Write(' ');
            if (instruction.OpCode == OpCodes.Ldtoken)
            {
                if (instruction.Operand is MethodReference)
                    writer.Write("method ");
                else if (instruction.Operand is FieldReference)
                    writer.Write("field ");
            }
            WriteOperand(writer, instruction.Operand);
        }
    }

    private static void WriteLabelList(ITextOutput writer, Instruction[] instructions)
    {
        writer.Write("(");
        for (var i = 0; i < instructions.Length; i++)
        {
            if (i != 0) writer.Write(", ");
            WriteOffsetReference(writer, instructions[i]);
        }
        writer.Write(")");
    }

    private static string ToInvariantCultureString(object value)
    {
        var convertible = value as IConvertible;
        return convertible?.ToString(CultureInfo.InvariantCulture) ?? value.ToString()!;
    }

    public static void WriteTo(this MethodReference method, ITextOutput writer)
    {
        if (method.ExplicitThis)
        {
            writer.Write("instance explicit ");
        }
        else if (method.HasThis)
        {
            writer.Write("instance ");
        }

        method.ReturnType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
        writer.Write(' ');
        if (method.DeclaringType != null)
        {
            method.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
            writer.Write("::");
        }

        if (method is MethodDefinition md && md.IsCompilerControlled)
        {
            writer.WriteReference(Escape(method.Name + "$PST" + method.MetadataToken.ToInt32().ToString("X8", CultureInfo.InvariantCulture)), method);
        }
        else
        {
            writer.WriteReference(Escape(method.Name), method);
        }

        if (method is GenericInstanceMethod gim)
        {
            writer.Write('<');
            for (var i = 0; i < gim.GenericArguments.Count; i++)
            {
                if (i > 0)
                    writer.Write(", ");
                gim.GenericArguments[i].WriteTo(writer);
            }
            writer.Write('>');
        }

        writer.Write("(");
        var parameters = method.Parameters;
        for (var i = 0; i < parameters.Count; ++i)
        {
            if (i > 0) writer.Write(", ");
            parameters[i].ParameterType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
        }
        writer.Write(")");
    }

    private static void WriteTo(this FieldReference field, ITextOutput writer)
    {
        field.FieldType.WriteTo(writer, ILNameSyntax.SignatureNoNamedTypeParameters);
        writer.Write(' ');
        field.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
        writer.Write("::");
        writer.WriteReference(Escape(field.Name), field);
    }

    private static bool IsValidIdentifierCharacter(char c)
    {
        return c == '_' || c == '$' || c == '@' || c == '?' || c == '`';
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;
        if (!(char.IsLetter(identifier[0]) || IsValidIdentifierCharacter(identifier[0])))
        {
            // As a special case, .ctor and .cctor are valid despite starting with a dot
            return identifier == ".ctor" || identifier == ".cctor";
        }
        for (var i = 1; i < identifier.Length; i++)
        {
            if (!(char.IsLetterOrDigit(identifier[i]) || IsValidIdentifierCharacter(identifier[i]) || identifier[i] == '.'))
                return false;
        }
        return true;
    }

    private static readonly HashSet<string> s_ilKeywords = BuildKeywordList(
        "abstract", "algorithm", "alignment", "ansi", "any", "arglist",
        "array", "as", "assembly", "assert", "at", "auto", "autochar", "beforefieldinit",
        "blob", "blob_object", "bool", "brnull", "brnull.s", "brzero", "brzero.s", "bstr",
        "bytearray", "byvalstr", "callmostderived", "carray", "catch", "cdecl", "cf",
        "char", "cil", "class", "clsid", "const", "currency", "custom", "date", "decimal",
        "default", "demand", "deny", "endmac", "enum", "error", "explicit", "extends", "extern",
        "false", "famandassem", "family", "famorassem", "fastcall", "fault", "field", "filetime",
        "filter", "final", "finally", "fixed", "float", "float32", "float64", "forwardref",
        "fromunmanaged", "handler", "hidebysig", "hresult", "idispatch", "il", "illegal",
        "implements", "implicitcom", "implicitres", "import", "in", "inheritcheck", "init",
        "initonly", "instance", "int", "int16", "int32", "int64", "int8", "interface", "internalcall",
        "iunknown", "lasterr", "lcid", "linkcheck", "literal", "localloc", "lpstr", "lpstruct", "lptstr",
        "lpvoid", "lpwstr", "managed", "marshal", "method", "modopt", "modreq", "native", "nested",
        "newslot", "noappdomain", "noinlining", "nomachine", "nomangle", "nometadata", "noncasdemand",
        "noncasinheritance", "noncaslinkdemand", "noprocess", "not", "not_in_gc_heap", "notremotable",
        "notserialized", "null", "nullref", "object", "objectref", "opt", "optil", "out",
        "permitonly", "pinned", "pinvokeimpl", "prefix1", "prefix2", "prefix3", "prefix4", "prefix5", "prefix6",
        "prefix7", "prefixref", "prejitdeny", "prejitgrant", "preservesig", "private", "privatescope", "protected",
        "public", "record", "refany", "reqmin", "reqopt", "reqrefuse", "reqsecobj", "request", "retval",
        "rtspecialname", "runtime", "safearray", "sealed", "sequential", "serializable", "special", "specialname",
        "static", "stdcall", "storage", "stored_object", "stream", "streamed_object", "string", "struct",
        "synchronized", "syschar", "sysstring", "tbstr", "thiscall", "tls", "to", "true", "typedref",
        "unicode", "unmanaged", "unmanagedexp", "unsigned", "unused", "userdefined", "value", "valuetype",
        "vararg", "variant", "vector", "virtual", "void", "wchar", "winapi", "with", "wrapper",

        // These are not listed as keywords in spec, but ILAsm treats them as such
        "property", "type", "flags", "callconv", "strict"
    );

    private static HashSet<string> BuildKeywordList(params string[] keywords)
    {
        var s = new HashSet<string>(keywords);
        foreach (var field in typeof(OpCodes).GetRuntimeFields())
        {
            if (field.FieldType == typeof(OpCode))
            {
                s.Add(((OpCode)field.GetValue(null)!).Name);
            }
        }
        return s;
    }

    public static string Escape(string identifier)
    {
        if (IsValidIdentifier(identifier) && !s_ilKeywords.Contains(identifier))
        {
            return identifier;
        }

        // The ECMA specification says that ' inside SQString should be ecaped using an octal escape sequence,
        // but we follow Microsoft's ILDasm and use \'.
        return "'" + TextWriterTokenWriter.ConvertString(identifier).Replace("'", "\\'") + "'";
    }

    public static void WriteTo(this TypeReference type, ITextOutput writer, ILNameSyntax syntax = ILNameSyntax.Signature)
    {
        var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;

        switch (type)
        {
            case PinnedType pinnedType:
                pinnedType.ElementType.WriteTo(writer, syntaxForElementTypes);
                writer.Write(" pinned");
                break;
            case ArrayType arrayType:
                arrayType.ElementType.WriteTo(writer, syntaxForElementTypes);
                writer.Write('[');
                writer.Write(string.Join(", ", arrayType.Dimensions));
                writer.Write(']');
                break;
            case GenericParameter genericParameter:
                writer.Write('!');
                if (genericParameter.Owner.GenericParameterType == GenericParameterType.Method)
                    writer.Write('!');
                if (string.IsNullOrEmpty(type.Name) || type.Name[0] == '!' || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
                    writer.Write(genericParameter.Position.ToString(CultureInfo.InvariantCulture));
                else
                    writer.Write(Escape(type.Name));
                break;
            case ByReferenceType byReferenceType:
                byReferenceType.ElementType.WriteTo(writer, syntaxForElementTypes);
                writer.Write('&');
                break;
            case PointerType pointerType:
                pointerType.ElementType.WriteTo(writer, syntaxForElementTypes);
                writer.Write('*');
                break;
            case GenericInstanceType genericInstanceType:
                {
                    type.GetElementType().WriteTo(writer, syntaxForElementTypes);
                    writer.Write('<');
                    var arguments = genericInstanceType.GenericArguments;
                    for (var i = 0; i < arguments.Count; i++)
                    {
                        if (i > 0)
                            writer.Write(", ");
                        arguments[i].WriteTo(writer, syntaxForElementTypes);
                    }
                    writer.Write('>');
                    break;
                }

            case OptionalModifierType optionalModifierType:
                optionalModifierType.ElementType.WriteTo(writer, syntax);
                writer.Write(" modopt(");
                optionalModifierType.ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
                writer.Write(") ");
                break;
            case RequiredModifierType requiredModifierType:
                requiredModifierType.ElementType.WriteTo(writer, syntax);
                writer.Write(" modreq(");
                requiredModifierType.ModifierType.WriteTo(writer, ILNameSyntax.TypeName);
                writer.Write(") ");
                break;
            default:
                var name = PrimitiveTypeName(type.FullName);
                if (syntax == ILNameSyntax.ShortTypeName)
                {
                    if (name != null)
                        writer.Write(name);
                    else
                        writer.WriteReference(Escape(type.Name), type);
                }
                else if ((syntax == ILNameSyntax.Signature || syntax == ILNameSyntax.SignatureNoNamedTypeParameters) && name != null)
                {
                    writer.Write(name);
                }
                else
                {
                    if (syntax == ILNameSyntax.Signature || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
                        writer.Write(type.IsValueType ? "valuetype " : "class ");

                    if (type.DeclaringType != null)
                    {
                        type.DeclaringType.WriteTo(writer, ILNameSyntax.TypeName);
                        writer.Write('/');
                        writer.WriteReference(Escape(type.Name), type);
                    }
                    else
                    {
                        if (!type.IsDefinition && type.Scope != null && type is not TypeSpecification)
                            writer.Write("[{0}]", Escape(type.Scope.Name));
                        writer.WriteReference(Escape(type.FullName), type);
                    }
                }

                break;
        }
    }

    public static void WriteOperand(ITextOutput writer, object operand)
    {
        ArgumentNullException.ThrowIfNull(operand);

        switch (operand)
        {
            case Instruction targetInstruction:
                WriteOffsetReference(writer, targetInstruction);
                return;
            case Instruction[] targetInstructions:
                WriteLabelList(writer, targetInstructions);
                return;
            case VariableReference variableRef:
                writer.WriteReference(
                    string.IsNullOrEmpty(variableRef.ToString())
                    ? variableRef.Index.ToString(CultureInfo.InvariantCulture)
                    : Escape(variableRef.ToString()), variableRef);
                return;
            case ParameterReference paramRef:
                writer.WriteReference(
                    string.IsNullOrEmpty(paramRef.Name) ? paramRef.Index.ToString(CultureInfo.InvariantCulture) : Escape(paramRef.Name), paramRef);
                return;
            case MethodReference methodRef:
                methodRef.WriteTo(writer);
                return;
            case TypeReference typeRef:
                typeRef.WriteTo(writer, ILNameSyntax.TypeName);
                return;
            case FieldReference fieldRef:
                fieldRef.WriteTo(writer);
                return;
            case string stringOpeand:
                writer.Write("\"" + TextWriterTokenWriter.ConvertString(stringOpeand) + "\"");
                break;
            case char charOperand:
                writer.Write(((int)charOperand).ToString(CultureInfo.InvariantCulture));
                break;
            case float floatOperand:
                if (floatOperand == 0)
                {
                    if (1 / floatOperand == float.NegativeInfinity)
                    {
                        // negative zero is a special case
                        writer.Write('-');
                    }
                    writer.Write("0.0");
                }
                else if (float.IsInfinity(floatOperand) || float.IsNaN(floatOperand))
                {
                    var data = BitConverter.GetBytes(floatOperand);
                    writer.Write('(');
                    for (var i = 0; i < data.Length; i++)
                    {
                        if (i > 0)
                            writer.Write(' ');
                        writer.Write(data[i].ToString("X2", CultureInfo.InvariantCulture));
                    }
                    writer.Write(')');
                }
                else
                {
                    writer.Write(floatOperand.ToString("R", CultureInfo.InvariantCulture));
                }

                break;

            case double doubleOperand:
                if (doubleOperand == 0)
                {
                    if (1 / doubleOperand == double.NegativeInfinity)
                    {
                        // negative zero is a special case
                        writer.Write('-');
                    }
                    writer.Write("0.0");
                }
                else if (double.IsInfinity(doubleOperand) || double.IsNaN(doubleOperand))
                {
                    var data = BitConverter.GetBytes(doubleOperand);
                    writer.Write('(');
                    for (var i = 0; i < data.Length; i++)
                    {
                        if (i > 0)
                            writer.Write(' ');
                        writer.Write(data[i].ToString("X2", CultureInfo.InvariantCulture));
                    }
                    writer.Write(')');
                }
                else
                {
                    writer.Write(doubleOperand.ToString("R", CultureInfo.InvariantCulture));
                }

                break;

            case bool boolOperand:
                writer.Write(boolOperand ? "true" : "false");
                break;
            default:
                writer.Write(ToInvariantCultureString(operand));
                break;
        }
    }

    public static string? PrimitiveTypeName(string fullName) => fullName switch
    {
        "System.SByte" => "int8",
        "System.Int16" => "int16",
        "System.Int32" => "int32",
        "System.Int64" => "int64",
        "System.Byte" => "uint8",
        "System.UInt16" => "uint16",
        "System.UInt32" => "uint32",
        "System.UInt64" => "uint64",
        "System.Single" => "float32",
        "System.Double" => "float64",
        "System.Void" => "void",
        "System.Boolean" => "bool",
        "System.String" => "string",
        "System.Char" => "char",
        "System.Object" => "object",
        "System.IntPtr" => "native int",
        _ => null,
    };
}
