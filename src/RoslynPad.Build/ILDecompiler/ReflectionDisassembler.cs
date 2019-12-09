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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace RoslynPad.Build.ILDecompiler
{
    /// <summary>
    /// Disassembles type and member definitions.
    /// </summary>
    internal sealed class ReflectionDisassembler
    {
        private readonly ITextOutput _output;
        private CancellationToken _cancellationToken;
        private bool _isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
        private readonly MethodBodyDisassembler _methodBodyDisassembler;

        public ReflectionDisassembler(ITextOutput output, bool detectControlStructure, CancellationToken cancellationToken)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _cancellationToken = cancellationToken;
            _methodBodyDisassembler = new MethodBodyDisassembler(output, detectControlStructure);
        }

        #region Disassemble Method

        private readonly EnumNameCollection<MethodAttributes> _methodAttributeFlags = new EnumNameCollection<MethodAttributes>
        {
            { MethodAttributes.Final, "final" },
            { MethodAttributes.HideBySig, "hidebysig" },
            { MethodAttributes.SpecialName, "specialname" },
            { MethodAttributes.PInvokeImpl, null }, // handled separately
			{ MethodAttributes.UnmanagedExport, "export" },
            { MethodAttributes.RTSpecialName, "rtspecialname" },
            { MethodAttributes.RequireSecObject, "reqsecobj" },
            { MethodAttributes.NewSlot, "newslot" },
            { MethodAttributes.CheckAccessOnOverride, "strict" },
            { MethodAttributes.Abstract, "abstract" },
            { MethodAttributes.Virtual, "virtual" },
            { MethodAttributes.Static, "static" },
            { MethodAttributes.HasSecurity, null } // ?? also invisible in ILDasm
		};

        private readonly EnumNameCollection<MethodAttributes> _methodVisibility = new EnumNameCollection<MethodAttributes>
        {
            { MethodAttributes.Private, "private" },
            { MethodAttributes.FamANDAssem, "famandassem" },
            { MethodAttributes.Assembly, "assembly" },
            { MethodAttributes.Family, "family" },
            { MethodAttributes.FamORAssem, "famorassem" },
            { MethodAttributes.Public, "public" }
        };

        private readonly EnumNameCollection<MethodCallingConvention> _callingConvention = new EnumNameCollection<MethodCallingConvention>
        {
            { MethodCallingConvention.C, "unmanaged cdecl" },
            { MethodCallingConvention.StdCall, "unmanaged stdcall" },
            { MethodCallingConvention.ThisCall, "unmanaged thiscall" },
            { MethodCallingConvention.FastCall, "unmanaged fastcall" },
            { MethodCallingConvention.VarArg, "vararg" },
            { MethodCallingConvention.Generic, null }
        };

        private readonly EnumNameCollection<MethodImplAttributes> _methodCodeType = new EnumNameCollection<MethodImplAttributes>
        {
            { MethodImplAttributes.IL, "cil" },
            { MethodImplAttributes.Native, "native" },
            { MethodImplAttributes.OPTIL, "optil" },
            { MethodImplAttributes.Runtime, "runtime" }
        };

        private readonly EnumNameCollection<MethodImplAttributes> _methodImpl = new EnumNameCollection<MethodImplAttributes>
        {
            { MethodImplAttributes.Synchronized, "synchronized" },
            { MethodImplAttributes.NoInlining, "noinlining" },
            { MethodImplAttributes.NoOptimization, "nooptimization" },
            { MethodImplAttributes.PreserveSig, "preservesig" },
            { MethodImplAttributes.InternalCall, "internalcall" },
            { MethodImplAttributes.ForwardRef, "forwardref" }
        };

        public void DisassembleMethod(MethodDefinition method)
        {
            // set current member

            // write method header
            _output.WriteDefinition(".method ", method);
            DisassembleMethodInternal(method);
        }

        private void DisassembleMethodInternal(MethodDefinition method)
        {
            //    .method public hidebysig  specialname
            //               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
            //

            //emit flags
            WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, _methodVisibility);
            WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, _methodAttributeFlags);
            if (method.IsCompilerControlled) _output.Write("privatescope ");

            if ((method.Attributes & MethodAttributes.PInvokeImpl) == MethodAttributes.PInvokeImpl)
            {
                _output.Write("pinvokeimpl");
                if (method.HasPInvokeInfo && method.PInvokeInfo != null)
                {
                    var info = method.PInvokeInfo;
                    _output.Write("(\"" + TextWriterTokenWriter.ConvertString(info.Module.Name) + "\"");

                    if (!string.IsNullOrEmpty(info.EntryPoint) && info.EntryPoint != method.Name)
                        _output.Write(" as \"" + TextWriterTokenWriter.ConvertString(info.EntryPoint) + "\"");

                    if (info.IsNoMangle)
                        _output.Write(" nomangle");

                    if (info.IsCharSetAnsi)
                        _output.Write(" ansi");
                    else if (info.IsCharSetAuto)
                        _output.Write(" autochar");
                    else if (info.IsCharSetUnicode)
                        _output.Write(" unicode");

                    if (info.SupportsLastError)
                        _output.Write(" lasterr");

                    if (info.IsCallConvCdecl)
                        _output.Write(" cdecl");
                    else if (info.IsCallConvFastcall)
                        _output.Write(" fastcall");
                    else if (info.IsCallConvStdCall)
                        _output.Write(" stdcall");
                    else if (info.IsCallConvThiscall)
                        _output.Write(" thiscall");
                    else if (info.IsCallConvWinapi)
                        _output.Write(" winapi");

                    _output.Write(')');
                }
                _output.Write(' ');
            }

            _output.WriteLine();
            _output.Indent();
            if (method.ExplicitThis)
            {
                _output.Write("instance explicit ");
            }
            else if (method.HasThis)
            {
                _output.Write("instance ");
            }

            //call convention
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            WriteEnum(method.CallingConvention & (MethodCallingConvention)0x1f, _callingConvention);

            //return type
            method.ReturnType.WriteTo(_output);
            _output.Write(' ');
            if (method.MethodReturnType.HasMarshalInfo)
            {
                WriteMarshalInfo(method.MethodReturnType.MarshalInfo);
            }

            _output.Write(method.IsCompilerControlled
                ? DisassemblerHelpers.Escape(method.Name + "$PST" + method.MetadataToken.ToInt32().ToString("X8"))
                : DisassemblerHelpers.Escape(method.Name));

            WriteTypeParameters(_output, method);

            //( params )
            _output.Write(" (");
            if (method.HasParameters)
            {
                _output.WriteLine();
                _output.Indent();
                WriteParameters(method.Parameters);
                _output.Unindent();
            }
            _output.Write(") ");
            //cil managed
            WriteEnum(method.ImplAttributes & MethodImplAttributes.CodeTypeMask, _methodCodeType);
            _output.Write((method.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed
                ? "managed "
                : "unmanaged ");
            WriteFlags(method.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), _methodImpl);

            _output.Unindent();
            OpenBlock(defaultCollapsed: _isInType);
            WriteAttributes(method.CustomAttributes);
            if (method.HasOverrides)
            {
                foreach (var methodOverride in method.Overrides)
                {
                    _output.Write(".override method ");
                    methodOverride.WriteTo(_output);
                    _output.WriteLine();
                }
            }
            WriteParameterAttributes(0, method.MethodReturnType, method.MethodReturnType);
            foreach (var p in method.Parameters)
            {
                WriteParameterAttributes(p.Index + 1, p, p);
            }
            WriteSecurityDeclarations(method);

            if (method.HasBody)
            {
                // create IL code mappings - used in debugger
                _methodBodyDisassembler.Disassemble(method.Body);
            }

            CloseBlock("end of method " + DisassemblerHelpers.Escape(method.DeclaringType.Name) + "::" + DisassemblerHelpers.Escape(method.Name));
        }

        #region Write Security Declarations

        private void WriteSecurityDeclarations(ISecurityDeclarationProvider secDeclProvider)
        {
            if (!secDeclProvider.HasSecurityDeclarations)
                return;
            foreach (var secdecl in secDeclProvider.SecurityDeclarations)
            {
                _output.Write(".permissionset ");
                switch (secdecl.Action)
                {
                    case SecurityAction.Request:
                        _output.Write("request");
                        break;
                    case SecurityAction.Demand:
                        _output.Write("demand");
                        break;
                    case SecurityAction.Assert:
                        _output.Write("assert");
                        break;
                    case SecurityAction.Deny:
                        _output.Write("deny");
                        break;
                    case SecurityAction.PermitOnly:
                        _output.Write("permitonly");
                        break;
                    case SecurityAction.LinkDemand:
                        _output.Write("linkcheck");
                        break;
                    case SecurityAction.InheritDemand:
                        _output.Write("inheritcheck");
                        break;
                    case SecurityAction.RequestMinimum:
                        _output.Write("reqmin");
                        break;
                    case SecurityAction.RequestOptional:
                        _output.Write("reqopt");
                        break;
                    case SecurityAction.RequestRefuse:
                        _output.Write("reqrefuse");
                        break;
                    case SecurityAction.PreJitGrant:
                        _output.Write("prejitgrant");
                        break;
                    case SecurityAction.PreJitDeny:
                        _output.Write("prejitdeny");
                        break;
                    case SecurityAction.NonCasDemand:
                        _output.Write("noncasdemand");
                        break;
                    case SecurityAction.NonCasLinkDemand:
                        _output.Write("noncaslinkdemand");
                        break;
                    case SecurityAction.NonCasInheritance:
                        _output.Write("noncasinheritance");
                        break;
                    default:
                        _output.Write(secdecl.Action.ToString());
                        break;
                }
                _output.WriteLine(" = {");
                _output.Indent();
                for (var i = 0; i < secdecl.SecurityAttributes.Count; i++)
                {
                    var sa = secdecl.SecurityAttributes[i];
                    if (sa.AttributeType.Scope == sa.AttributeType.Module)
                    {
                        _output.Write("class ");
                        _output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(sa.AttributeType)));
                    }
                    else
                    {
                        sa.AttributeType.WriteTo(_output, ILNameSyntax.TypeName);
                    }
                    _output.Write(" = {");
                    if (sa.HasFields || sa.HasProperties)
                    {
                        _output.WriteLine();
                        _output.Indent();

                        foreach (var na in sa.Fields)
                        {
                            _output.Write("field ");
                            WriteSecurityDeclarationArgument(na);
                            _output.WriteLine();
                        }

                        foreach (var na in sa.Properties)
                        {
                            _output.Write("property ");
                            WriteSecurityDeclarationArgument(na);
                            _output.WriteLine();
                        }

                        _output.Unindent();
                    }
                    _output.Write('}');

                    if (i + 1 < secdecl.SecurityAttributes.Count)
                        _output.Write(',');
                    _output.WriteLine();
                }
                _output.Unindent();
                _output.WriteLine("}");
            }
        }

        private void WriteSecurityDeclarationArgument(CustomAttributeNamedArgument na)
        {
            var type = na.Argument.Type;
            if (type.MetadataType == MetadataType.Class || type.MetadataType == MetadataType.ValueType)
            {
                _output.Write("enum ");
                if (type.Scope != type.Module)
                {
                    _output.Write("class ");
                    _output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(type)));
                }
                else
                {
                    type.WriteTo(_output, ILNameSyntax.TypeName);
                }
            }
            else
            {
                type.WriteTo(_output);
            }
            _output.Write(' ');
            _output.Write(DisassemblerHelpers.Escape(na.Name));
            _output.Write(" = ");
            if (na.Argument.Value is string value)
            {
                // secdecls use special syntax for strings
                _output.Write("string('{0}')", TextWriterTokenWriter.ConvertString(value).Replace("'", "\'"));
            }
            else
            {
                WriteConstant(na.Argument.Value);
            }
        }

        private static string GetAssemblyQualifiedName(TypeReference type)
        {
            var anr = type.Scope as AssemblyNameReference;
            if (anr == null)
            {
                if (type.Scope is ModuleDefinition md)
                {
                    anr = md.Assembly.Name;
                }
            }
            if (anr != null)
            {
                return type.FullName + ", " + anr.FullName;
            }
            return type.FullName;
        }
        #endregion

        #region WriteMarshalInfo

        private void WriteMarshalInfo(MarshalInfo marshalInfo)
        {
            _output.Write("marshal(");
            WriteNativeType(marshalInfo.NativeType, marshalInfo);
            _output.Write(") ");
        }

        private void WriteNativeType(NativeType nativeType, MarshalInfo? marshalInfo = null)
        {
            switch (nativeType)
            {
                case NativeType.None:
                    break;
                case NativeType.Boolean:
                    _output.Write("bool");
                    break;
                case NativeType.I1:
                    _output.Write("int8");
                    break;
                case NativeType.U1:
                    _output.Write("unsigned int8");
                    break;
                case NativeType.I2:
                    _output.Write("int16");
                    break;
                case NativeType.U2:
                    _output.Write("unsigned int16");
                    break;
                case NativeType.I4:
                    _output.Write("int32");
                    break;
                case NativeType.U4:
                    _output.Write("unsigned int32");
                    break;
                case NativeType.I8:
                    _output.Write("int64");
                    break;
                case NativeType.U8:
                    _output.Write("unsigned int64");
                    break;
                case NativeType.R4:
                    _output.Write("float32");
                    break;
                case NativeType.R8:
                    _output.Write("float64");
                    break;
                case NativeType.LPStr:
                    _output.Write("lpstr");
                    break;
                case NativeType.Int:
                    _output.Write("int");
                    break;
                case NativeType.UInt:
                    _output.Write("unsigned int");
                    break;
                case NativeType.Func:
                    goto default; // ??
                case NativeType.Array:
                    var ami = marshalInfo as ArrayMarshalInfo;
                    if (ami == null)
                        goto default;
                    if (ami.ElementType != NativeType.Max)
                        WriteNativeType(ami.ElementType);
                    _output.Write('[');
                    if (ami.SizeParameterMultiplier == 0)
                    {
                        _output.Write(ami.Size.ToString());
                    }
                    else
                    {
                        if (ami.Size >= 0)
                            _output.Write(ami.Size.ToString());
                        _output.Write(" + ");
                        _output.Write(ami.SizeParameterIndex.ToString());
                    }
                    _output.Write(']');
                    break;
                case NativeType.Currency:
                    _output.Write("currency");
                    break;
                case NativeType.BStr:
                    _output.Write("bstr");
                    break;
                case NativeType.LPWStr:
                    _output.Write("lpwstr");
                    break;
                case NativeType.LPTStr:
                    _output.Write("lptstr");
                    break;
                case NativeType.FixedSysString:
                    _output.Write("fixed sysstring[{0}]", (marshalInfo as FixedSysStringMarshalInfo)?.Size ?? 0);
                    break;
                case NativeType.IUnknown:
                    _output.Write("iunknown");
                    break;
                case NativeType.IDispatch:
                    _output.Write("idispatch");
                    break;
                case NativeType.Struct:
                    _output.Write("struct");
                    break;
                case NativeType.IntF:
                    _output.Write("interface");
                    break;
                case NativeType.SafeArray:
                    _output.Write("safearray ");
                    var sami = marshalInfo as SafeArrayMarshalInfo;
                    if (sami != null)
                    {
                        switch (sami.ElementType)
                        {
                            case VariantType.None:
                                break;
                            case VariantType.I2:
                                _output.Write("int16");
                                break;
                            case VariantType.I4:
                                _output.Write("int32");
                                break;
                            case VariantType.R4:
                                _output.Write("float32");
                                break;
                            case VariantType.R8:
                                _output.Write("float64");
                                break;
                            case VariantType.CY:
                                _output.Write("currency");
                                break;
                            case VariantType.Date:
                                _output.Write("date");
                                break;
                            case VariantType.BStr:
                                _output.Write("bstr");
                                break;
                            case VariantType.Dispatch:
                                _output.Write("idispatch");
                                break;
                            case VariantType.Error:
                                _output.Write("error");
                                break;
                            case VariantType.Bool:
                                _output.Write("bool");
                                break;
                            case VariantType.Variant:
                                _output.Write("variant");
                                break;
                            case VariantType.Unknown:
                                _output.Write("iunknown");
                                break;
                            case VariantType.Decimal:
                                _output.Write("decimal");
                                break;
                            case VariantType.I1:
                                _output.Write("int8");
                                break;
                            case VariantType.UI1:
                                _output.Write("unsigned int8");
                                break;
                            case VariantType.UI2:
                                _output.Write("unsigned int16");
                                break;
                            case VariantType.UI4:
                                _output.Write("unsigned int32");
                                break;
                            case VariantType.Int:
                                _output.Write("int");
                                break;
                            case VariantType.UInt:
                                _output.Write("unsigned int");
                                break;
                            default:
                                _output.Write(sami.ElementType.ToString());
                                break;
                        }
                    }
                    break;
                case NativeType.FixedArray:
                    _output.Write("fixed array");
                    var fami = marshalInfo as FixedArrayMarshalInfo;
                    if (fami != null)
                    {
                        _output.Write("[{0}]", fami.Size);
                        if (fami.ElementType != NativeType.None)
                        {
                            _output.Write(' ');
                            WriteNativeType(fami.ElementType);
                        }
                    }
                    break;
                case NativeType.ByValStr:
                    _output.Write("byvalstr");
                    break;
                case NativeType.ANSIBStr:
                    _output.Write("ansi bstr");
                    break;
                case NativeType.TBStr:
                    _output.Write("tbstr");
                    break;
                case NativeType.VariantBool:
                    _output.Write("variant bool");
                    break;
                case NativeType.ASAny:
                    _output.Write("as any");
                    break;
                case NativeType.LPStruct:
                    _output.Write("lpstruct");
                    break;
                case NativeType.CustomMarshaler:
                    var cmi = marshalInfo as CustomMarshalInfo;
                    if (cmi == null)
                        goto default;
                    _output.Write("custom(\"{0}\", \"{1}\"",
                                 TextWriterTokenWriter.ConvertString(cmi.ManagedType.FullName),
                                 TextWriterTokenWriter.ConvertString(cmi.Cookie));
                    if (cmi.Guid != Guid.Empty || !string.IsNullOrEmpty(cmi.UnmanagedType))
                    {
                        _output.Write(", \"{0}\", \"{1}\"", cmi.Guid.ToString(), TextWriterTokenWriter.ConvertString(cmi.UnmanagedType));
                    }
                    _output.Write(')');
                    break;
                case NativeType.Error:
                    _output.Write("error");
                    break;
                default:
                    _output.Write(nativeType.ToString());
                    break;
            }
        }
        #endregion

        private void WriteParameters(Collection<ParameterDefinition> parameters)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                if (p.IsIn)
                    _output.Write("[in] ");
                if (p.IsOut)
                    _output.Write("[out] ");
                if (p.IsOptional)
                    _output.Write("[opt] ");
                p.ParameterType.WriteTo(_output);
                _output.Write(' ');
                if (p.HasMarshalInfo)
                {
                    WriteMarshalInfo(p.MarshalInfo);
                }
                _output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p);
                if (i < parameters.Count - 1)
                    _output.Write(',');
                _output.WriteLine();
            }
        }

        private void WriteParameterAttributes(int index, IConstantProvider cp, ICustomAttributeProvider cap)
        {
            if (!cp.HasConstant && !cap.HasCustomAttributes)
                return;
            _output.Write(".param [{0}]", index);
            if (cp.HasConstant)
            {
                _output.Write(" = ");
                WriteConstant(cp.Constant);
            }
            _output.WriteLine();
            WriteAttributes(cap.CustomAttributes);
        }

        private void WriteConstant(object constant)
        {
            if (constant == null)
            {
                _output.Write("nullref");
            }
            else
            {
                var typeName = DisassemblerHelpers.PrimitiveTypeName(constant.GetType().FullName!);
                if (typeName != null && typeName != "string")
                {
                    _output.Write(typeName);
                    _output.Write('(');
                    var cf = constant as float?;
                    var cd = constant as double?;
                    if (cf.HasValue && (float.IsNaN(cf.Value) || float.IsInfinity(cf.Value)))
                    {
                        _output.Write("0x{0:x8}", BitConverter.ToInt32(BitConverter.GetBytes(cf.Value), 0));
                    }
                    else if (cd.HasValue && (double.IsNaN(cd.Value) || double.IsInfinity(cd.Value)))
                    {
                        _output.Write("0x{0:x16}", BitConverter.DoubleToInt64Bits(cd.Value));
                    }
                    else
                    {
                        DisassemblerHelpers.WriteOperand(_output, constant);
                    }
                    _output.Write(')');
                }
                else
                {
                    DisassemblerHelpers.WriteOperand(_output, constant);
                }
            }
        }
        #endregion

        #region Disassemble Field

        private readonly EnumNameCollection<FieldAttributes> _fieldVisibility = new EnumNameCollection<FieldAttributes>
        {
            { FieldAttributes.Private, "private" },
            { FieldAttributes.FamANDAssem, "famandassem" },
            { FieldAttributes.Assembly, "assembly" },
            { FieldAttributes.Family, "family" },
            { FieldAttributes.FamORAssem, "famorassem" },
            { FieldAttributes.Public, "public" }
        };

        private readonly EnumNameCollection<FieldAttributes> _fieldAttributes = new EnumNameCollection<FieldAttributes>
        {
            { FieldAttributes.Static, "static" },
            { FieldAttributes.Literal, "literal" },
            { FieldAttributes.InitOnly, "initonly" },
            { FieldAttributes.SpecialName, "specialname" },
            { FieldAttributes.RTSpecialName, "rtspecialname" },
            { FieldAttributes.NotSerialized, "notserialized" }
        };

        public void DisassembleField(FieldDefinition field)
        {
            _output.WriteDefinition(".field ", field);
            if (field.HasLayoutInfo)
            {
                _output.Write("[" + field.Offset + "] ");
            }
            WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, _fieldVisibility);
            const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
            WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), _fieldAttributes);
            if (field.HasMarshalInfo)
            {
                WriteMarshalInfo(field.MarshalInfo);
            }
            field.FieldType.WriteTo(_output);
            _output.Write(' ');
            _output.Write(DisassemblerHelpers.Escape(field.Name));
            if ((field.Attributes & FieldAttributes.HasFieldRVA) == FieldAttributes.HasFieldRVA)
            {
                _output.Write(" at I_{0:x8}", field.RVA);
            }
            if (field.HasConstant)
            {
                _output.Write(" = ");
                WriteConstant(field.Constant);
            }
            _output.WriteLine();
            if (field.HasCustomAttributes)
            {
                _output.MarkFoldStart();
                WriteAttributes(field.CustomAttributes);
                _output.MarkFoldEnd();
            }
        }
        #endregion

        #region Disassemble Property

        private readonly EnumNameCollection<PropertyAttributes> _propertyAttributes = new EnumNameCollection<PropertyAttributes>
        {
            { PropertyAttributes.SpecialName, "specialname" },
            { PropertyAttributes.RTSpecialName, "rtspecialname" },
            { PropertyAttributes.HasDefault, "hasdefault" }
        };

        public void DisassembleProperty(PropertyDefinition property)
        {
            // set current member

            _output.WriteDefinition(".property ", property);
            WriteFlags(property.Attributes, _propertyAttributes);
            if (property.HasThis)
                _output.Write("instance ");
            property.PropertyType.WriteTo(_output);
            _output.Write(' ');
            _output.Write(DisassemblerHelpers.Escape(property.Name));

            _output.Write("(");
            if (property.HasParameters)
            {
                _output.WriteLine();
                _output.Indent();
                WriteParameters(property.Parameters);
                _output.Unindent();
            }
            _output.Write(")");

            OpenBlock(false);
            WriteAttributes(property.CustomAttributes);
            WriteNestedMethod(".get", property.GetMethod);
            WriteNestedMethod(".set", property.SetMethod);

            foreach (var method in property.OtherMethods)
            {
                WriteNestedMethod(".other", method);
            }
            CloseBlock();
        }

        private void WriteNestedMethod(string keyword, MethodDefinition method)
        {
            if (method == null)
                return;

            _output.Write(keyword);
            _output.Write(' ');
            method.WriteTo(_output);
            _output.WriteLine();
        }
        #endregion

        #region Disassemble Event

        private readonly EnumNameCollection<EventAttributes> _eventAttributes = new EnumNameCollection<EventAttributes>
        {
            { EventAttributes.SpecialName, "specialname" },
            { EventAttributes.RTSpecialName, "rtspecialname" }
        };

        public void DisassembleEvent(EventDefinition ev)
        {
            // set current member

            _output.WriteDefinition(".event ", ev);
            WriteFlags(ev.Attributes, _eventAttributes);
            ev.EventType.WriteTo(_output, ILNameSyntax.TypeName);
            _output.Write(' ');
            _output.Write(DisassemblerHelpers.Escape(ev.Name));
            OpenBlock(false);
            WriteAttributes(ev.CustomAttributes);
            WriteNestedMethod(".addon", ev.AddMethod);
            WriteNestedMethod(".removeon", ev.RemoveMethod);
            WriteNestedMethod(".fire", ev.InvokeMethod);
            foreach (var method in ev.OtherMethods)
            {
                WriteNestedMethod(".other", method);
            }
            CloseBlock();
        }
        #endregion

        #region Disassemble Type

        private readonly EnumNameCollection<TypeAttributes> _typeVisibility = new EnumNameCollection<TypeAttributes>
        {
            { TypeAttributes.Public, "public" },
            { TypeAttributes.NotPublic, "private" },
            { TypeAttributes.NestedPublic, "nested public" },
            { TypeAttributes.NestedPrivate, "nested private" },
            { TypeAttributes.NestedAssembly, "nested assembly" },
            { TypeAttributes.NestedFamily, "nested family" },
            { TypeAttributes.NestedFamANDAssem, "nested famandassem" },
            { TypeAttributes.NestedFamORAssem, "nested famorassem" }
        };

        private readonly EnumNameCollection<TypeAttributes> _typeLayout = new EnumNameCollection<TypeAttributes>
        {
            { TypeAttributes.AutoLayout, "auto" },
            { TypeAttributes.SequentialLayout, "sequential" },
            { TypeAttributes.ExplicitLayout, "explicit" }
        };

        private readonly EnumNameCollection<TypeAttributes> _typeStringFormat = new EnumNameCollection<TypeAttributes>
        {
            { TypeAttributes.AutoClass, "auto" },
            { TypeAttributes.AnsiClass, "ansi" },
            { TypeAttributes.UnicodeClass, "unicode" }
        };

        private readonly EnumNameCollection<TypeAttributes> _typeAttributes = new EnumNameCollection<TypeAttributes>
        {
            { TypeAttributes.Abstract, "abstract" },
            { TypeAttributes.Sealed, "sealed" },
            { TypeAttributes.SpecialName, "specialname" },
            { TypeAttributes.Import, "import" },
            { TypeAttributes.Serializable, "serializable" },
            { TypeAttributes.WindowsRuntime, "windowsruntime" },
            { TypeAttributes.BeforeFieldInit, "beforefieldinit" },
            { TypeAttributes.HasSecurity, null }
        };

        public void DisassembleType(TypeDefinition type)
        {
            // start writing IL
            _output.WriteDefinition(".class ", type);

            if ((type.Attributes & TypeAttributes.ClassSemanticMask) == TypeAttributes.Interface)
                _output.Write("interface ");
            WriteEnum(type.Attributes & TypeAttributes.VisibilityMask, _typeVisibility);
            WriteEnum(type.Attributes & TypeAttributes.LayoutMask, _typeLayout);
            WriteEnum(type.Attributes & TypeAttributes.StringFormatMask, _typeStringFormat);
            const TypeAttributes masks = TypeAttributes.ClassSemanticMask | TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.StringFormatMask;
            WriteFlags(type.Attributes & ~masks, _typeAttributes);

            _output.Write(DisassemblerHelpers.Escape(type.DeclaringType != null ? type.Name : type.FullName));
            WriteTypeParameters(_output, type);
            _output.MarkFoldStart(defaultCollapsed: _isInType);
            _output.WriteLine();

            if (type.BaseType != null)
            {
                _output.Indent();
                _output.Write("extends ");
                type.BaseType.WriteTo(_output, ILNameSyntax.TypeName);
                _output.WriteLine();
                _output.Unindent();
            }
            if (type.HasInterfaces)
            {
                _output.Indent();
                for (var index = 0; index < type.Interfaces.Count; index++)
                {
                    if (index > 0)
                    {
                        _output.WriteLine(",");
                    }
                    _output.Write(index == 0 ? "implements " : "           ");
                    type.Interfaces[index].InterfaceType.WriteTo(_output, ILNameSyntax.TypeName);
                }
                _output.WriteLine();
                _output.Unindent();
            }

            _output.WriteLine("{");
            _output.Indent();
            var oldIsInType = _isInType;
            _isInType = true;
            WriteAttributes(type.CustomAttributes);
            WriteSecurityDeclarations(type);
            if (type.HasLayoutInfo)
            {
                _output.WriteLine(".pack {0}", type.PackingSize);
                _output.WriteLine(".size {0}", type.ClassSize);
                _output.WriteLine();
            }
            if (type.HasNestedTypes)
            {
                _output.WriteLine("// Nested Types");
                foreach (var nestedType in type.NestedTypes)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    DisassembleType(nestedType);
                    _output.WriteLine();
                }
                _output.WriteLine();
            }
            if (type.HasFields)
            {
                _output.WriteLine("// Fields");
                foreach (var field in type.Fields)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    DisassembleField(field);
                }
                _output.WriteLine();
            }
            if (type.HasMethods)
            {
                _output.WriteLine("// Methods");
                foreach (var m in type.Methods)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    DisassembleMethod(m);
                    _output.WriteLine();
                }
            }
            if (type.HasEvents)
            {
                _output.WriteLine("// Events");
                foreach (var ev in type.Events)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    DisassembleEvent(ev);
                    _output.WriteLine();
                }
                _output.WriteLine();
            }
            if (type.HasProperties)
            {
                _output.WriteLine("// Properties");
                foreach (var prop in type.Properties)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    DisassembleProperty(prop);
                }
                _output.WriteLine();
            }
            CloseBlock("end of class " + (type.DeclaringType != null ? type.Name : type.FullName));
            _isInType = oldIsInType;
        }

        private void WriteTypeParameters(ITextOutput output, IGenericParameterProvider p)
        {
            if (p.HasGenericParameters)
            {
                output.Write('<');
                for (var i = 0; i < p.GenericParameters.Count; i++)
                {
                    if (i > 0)
                        output.Write(", ");
                    var gp = p.GenericParameters[i];
                    if (gp.HasReferenceTypeConstraint)
                    {
                        output.Write("class ");
                    }
                    else if (gp.HasNotNullableValueTypeConstraint)
                    {
                        output.Write("valuetype ");
                    }
                    if (gp.HasDefaultConstructorConstraint)
                    {
                        output.Write(".ctor ");
                    }
                    if (gp.HasConstraints)
                    {
                        output.Write('(');
                        for (var j = 0; j < gp.Constraints.Count; j++)
                        {
                            if (j > 0)
                                output.Write(", ");
                            gp.Constraints[j].WriteTo(output, ILNameSyntax.TypeName);
                        }
                        output.Write(") ");
                    }
                    if (gp.IsContravariant)
                    {
                        output.Write('-');
                    }
                    else if (gp.IsCovariant)
                    {
                        output.Write('+');
                    }
                    output.Write(DisassemblerHelpers.Escape(gp.Name));
                }
                output.Write('>');
            }
        }
        #endregion

        #region Helper methods

        private void WriteAttributes(Collection<CustomAttribute> attributes)
        {
            foreach (var a in attributes)
            {
                _output.Write(".custom ");
                a.Constructor.WriteTo(_output);
                var blob = a.GetBlob();
                if (blob != null)
                {
                    _output.Write(" = ");
                    WriteBlob(blob);
                }
                _output.WriteLine();
            }
        }

        private void WriteBlob(byte[] blob)
        {
            _output.Write("(");
            _output.Indent();

            for (var i = 0; i < blob.Length; i++)
            {
                if (i % 16 == 0 && i < blob.Length - 1)
                {
                    _output.WriteLine();
                }
                else
                {
                    _output.Write(' ');
                }
                _output.Write(blob[i].ToString("x2"));
            }

            _output.WriteLine();
            _output.Unindent();
            _output.Write(")");
        }

        private void OpenBlock(bool defaultCollapsed)
        {
            _output.MarkFoldStart(defaultCollapsed: defaultCollapsed);
            _output.WriteLine();
            _output.WriteLine("{");
            _output.Indent();
        }

        private void CloseBlock(string? comment = null)
        {
            _output.Unindent();
            _output.Write("}");
            if (comment != null)
                _output.Write(" // " + comment);
            _output.MarkFoldEnd();
            _output.WriteLine();
        }

        private void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
        {
            var val = Convert.ToInt64(flags);
            long tested = 0;
            foreach (var pair in flagNames)
            {
                tested |= pair.Key;
                if ((val & pair.Key) != 0 && pair.Value != null)
                {
                    _output.Write(pair.Value);
                    _output.Write(' ');
                }
            }
            if ((val & ~tested) != 0)
                _output.Write("flag({0:x4}) ", val & ~tested);
        }

        private void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
        {
            var val = Convert.ToInt64(enumValue);
            foreach (var pair in enumNames)
            {
                if (pair.Key == val)
                {
                    if (pair.Value != null)
                    {
                        _output.Write(pair.Value);
                        _output.Write(' ');
                    }
                    return;
                }
            }
            if (val != 0)
            {
                _output.Write("flag({0:x4})", val);
                _output.Write(' ');
            }

        }

        private sealed class EnumNameCollection<T> : IEnumerable<KeyValuePair<long, string?>> where T : struct
        {
            private readonly List<KeyValuePair<long, string?>> _names = new List<KeyValuePair<long, string?>>();

            public void Add(T flag, string? name)
            {
                _names.Add(new KeyValuePair<long, string?>(Convert.ToInt64(flag), name));
            }

            public IEnumerator<KeyValuePair<long, string?>> GetEnumerator()
            {
                return _names.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _names.GetEnumerator();
            }
        }
        #endregion

        public void DisassembleNamespace(string nameSpace, IEnumerable<TypeDefinition> types)
        {
            if (!string.IsNullOrEmpty(nameSpace))
            {
                _output.Write(".namespace " + DisassemblerHelpers.Escape(nameSpace));
                OpenBlock(false);
            }
            var oldIsInType = _isInType;
            _isInType = true;
            foreach (var td in types)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                DisassembleType(td);
                _output.WriteLine();
            }
            if (!string.IsNullOrEmpty(nameSpace))
            {
                CloseBlock();
                _isInType = oldIsInType;
            }
        }

        public void WriteAssemblyHeader(AssemblyDefinition asm)
        {
            _output.Write(".assembly ");
            if (asm.Name.IsWindowsRuntime)
                _output.Write("windowsruntime ");
            _output.Write(DisassemblerHelpers.Escape(asm.Name.Name));
            OpenBlock(false);
            WriteAttributes(asm.CustomAttributes);
            WriteSecurityDeclarations(asm);
            if (asm.Name.PublicKey != null && asm.Name.PublicKey.Length > 0)
            {
                _output.Write(".publickey = ");
                WriteBlob(asm.Name.PublicKey);
                _output.WriteLine();
            }
            if (asm.Name.HashAlgorithm != AssemblyHashAlgorithm.None)
            {
                _output.Write(".hash algorithm 0x{0:x8}", (int)asm.Name.HashAlgorithm);
                if (asm.Name.HashAlgorithm == AssemblyHashAlgorithm.SHA1)
                    _output.Write(" // SHA1");
                _output.WriteLine();
            }
            var v = asm.Name.Version;
            if (v != null)
            {
                _output.WriteLine(".ver {0}:{1}:{2}:{3}", v.Major, v.Minor, v.Build, v.Revision);
            }
            CloseBlock();
        }

        public void WriteAssemblyReferences(ModuleDefinition module)
        {
            foreach (var mref in module.ModuleReferences)
            {
                _output.WriteLine(".module extern {0}", DisassemblerHelpers.Escape(mref.Name));
            }
            foreach (var aref in module.AssemblyReferences)
            {
                _output.Write(".assembly extern ");
                if (aref.IsWindowsRuntime)
                    _output.Write("windowsruntime ");
                _output.Write(DisassemblerHelpers.Escape(aref.Name));
                OpenBlock(false);
                if (aref.PublicKeyToken != null)
                {
                    _output.Write(".publickeytoken = ");
                    WriteBlob(aref.PublicKeyToken);
                    _output.WriteLine();
                }
                if (aref.Version != null)
                {
                    _output.WriteLine(".ver {0}:{1}:{2}:{3}", aref.Version.Major, aref.Version.Minor, aref.Version.Build, aref.Version.Revision);
                }
                CloseBlock();
            }
        }

        public void WriteModuleHeader(ModuleDefinition module)
        {
            if (module.HasExportedTypes)
            {
                foreach (var exportedType in module.ExportedTypes)
                {
                    _output.Write(".class extern ");
                    if (exportedType.IsForwarder)
                        _output.Write("forwarder ");
                    _output.Write(exportedType.DeclaringType != null ? exportedType.Name : exportedType.FullName);
                    OpenBlock(false);
                    if (exportedType.DeclaringType != null)
                        _output.WriteLine(".class extern {0}", DisassemblerHelpers.Escape(exportedType.DeclaringType.FullName));
                    else
                        _output.WriteLine(".assembly extern {0}", DisassemblerHelpers.Escape(exportedType.Scope.Name));
                    CloseBlock();
                }
            }

            _output.WriteLine(".module {0}", module.Name);
            _output.WriteLine("// MVID: {0}", module.Mvid.ToString("B").ToUpperInvariant());
            // TODO: imagebase, file alignment, stackreserve, subsystem
            _output.WriteLine(".corflags 0x{0:x} // {1}", module.Attributes, module.Attributes.ToString());

            WriteAttributes(module.CustomAttributes);
        }

        public void WriteModuleContents(ModuleDefinition module)
        {
            foreach (var td in module.Types)
            {
                DisassembleType(td);
                _output.WriteLine();
            }
        }
    }
}
