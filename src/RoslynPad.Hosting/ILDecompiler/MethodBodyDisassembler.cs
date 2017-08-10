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
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace RoslynPad.Hosting.ILDecompiler
{
    /// <summary>
    /// Disassembles a method body.
    /// </summary>
    internal sealed class MethodBodyDisassembler
    {
        private readonly ITextOutput _output;
        private readonly bool _detectControlStructure;

        public MethodBodyDisassembler(ITextOutput output, bool detectControlStructure)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _detectControlStructure = detectControlStructure;
        }

        public void Disassemble(MethodBody body)
        {
            // start writing IL code
            var method = body.Method;
            _output.WriteLine("// Method begins at RVA 0x{0:x4}", method.RVA);
            _output.WriteLine("// Code size {0} (0x{0:x})", body.CodeSize);
            _output.WriteLine(".maxstack {0}", body.MaxStackSize);
            if (method.DeclaringType.Module.Assembly != null && method.DeclaringType.Module.Assembly.EntryPoint == method)
                _output.WriteLine(".entrypoint");

            if (method.Body.HasVariables)
            {
                _output.Write(".locals ");
                if (method.Body.InitLocals)
                    _output.Write("init ");
                _output.WriteLine("(");
                _output.Indent();
                foreach (var v in method.Body.Variables)
                {
                    _output.WriteDefinition("[" + v.Index + "] ", v);
                    v.VariableType.WriteTo(_output);
                    if (!string.IsNullOrEmpty(v.ToString()))
                    {
                        _output.Write(' ');
                        _output.Write(DisassemblerHelpers.Escape(v.ToString()));
                    }
                    if (v.Index + 1 < method.Body.Variables.Count)
                        _output.Write(',');
                    _output.WriteLine();
                }
                _output.Unindent();
                _output.WriteLine(")");
            }
            _output.WriteLine();

            if (_detectControlStructure && body.Instructions.Count > 0)
            {
                var inst = body.Instructions[0];
                var branchTargets = GetBranchTargets(body.Instructions);
                WriteStructureBody(new ILStructure(body), branchTargets, ref inst);
            }
            else
            {
                foreach (var inst in method.Body.Instructions)
                {
                    inst.WriteTo(_output);
                    _output.WriteLine();
                }

                if (method.Body.HasExceptionHandlers)
                {
                    _output.WriteLine();
                    foreach (var eh in method.Body.ExceptionHandlers)
                    {
                        eh.WriteTo(_output);
                        _output.WriteLine();
                    }
                }
            }
        }

        private HashSet<int> GetBranchTargets(IEnumerable<Instruction> instructions)
        {
            var branchTargets = new HashSet<int>();
            foreach (var inst in instructions)
            {
                if (inst.Operand is Instruction target)
                    branchTargets.Add(target.Offset);

                if (inst.Operand is Instruction[] targets)
                    foreach (var t in targets)
                        branchTargets.Add(t.Offset);
            }
            return branchTargets;
        }

        private void WriteStructureHeader(ILStructure s)
        {
            switch (s.Type)
            {
                case ILStructureType.Loop:
                    _output.Write("// loop start");
                    if (s.LoopEntryPoint != null)
                    {
                        _output.Write(" (head: ");
                        DisassemblerHelpers.WriteOffsetReference(_output, s.LoopEntryPoint);
                        _output.Write(')');
                    }
                    _output.WriteLine();
                    break;
                case ILStructureType.Try:
                    _output.WriteLine(".try");
                    _output.WriteLine("{");
                    break;
                case ILStructureType.Handler:
                    switch (s.ExceptionHandler.HandlerType)
                    {
                        case ExceptionHandlerType.Catch:
                        case ExceptionHandlerType.Filter:
                            _output.Write("catch");
                            if (s.ExceptionHandler.CatchType != null)
                            {
                                _output.Write(' ');
                                s.ExceptionHandler.CatchType.WriteTo(_output, ILNameSyntax.TypeName);
                            }
                            _output.WriteLine();
                            break;
                        case ExceptionHandlerType.Finally:
                            _output.WriteLine("finally");
                            break;
                        case ExceptionHandlerType.Fault:
                            _output.WriteLine("fault");
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    _output.WriteLine("{");
                    break;
                case ILStructureType.Filter:
                    _output.WriteLine("filter");
                    _output.WriteLine("{");
                    break;
                default:
                    throw new NotSupportedException();
            }
            _output.Indent();
        }

        private void WriteStructureBody(ILStructure s, HashSet<int> branchTargets, ref Instruction inst)
        {
            var isFirstInstructionInStructure = true;
            var prevInstructionWasBranch = false;
            var childIndex = 0;
            while (inst != null && inst.Offset < s.EndOffset)
            {
                var offset = inst.Offset;
                if (childIndex < s.Children.Count && s.Children[childIndex].StartOffset <= offset && offset < s.Children[childIndex].EndOffset)
                {
                    var child = s.Children[childIndex++];
                    WriteStructureHeader(child);
                    WriteStructureBody(child, branchTargets, ref inst);
                    WriteStructureFooter(child);
                }
                else
                {
                    if (!isFirstInstructionInStructure && (prevInstructionWasBranch || branchTargets.Contains(offset)))
                    {
                        _output.WriteLine(); // put an empty line after branches, and in front of branch targets
                    }

                    inst.WriteTo(_output);
                    _output.WriteLine();

                    prevInstructionWasBranch = inst.OpCode.FlowControl == FlowControl.Branch
                        || inst.OpCode.FlowControl == FlowControl.Cond_Branch
                        || inst.OpCode.FlowControl == FlowControl.Return
                        || inst.OpCode.FlowControl == FlowControl.Throw;

                    inst = inst.Next;
                }
                isFirstInstructionInStructure = false;
            }
        }

        private void WriteStructureFooter(ILStructure s)
        {
            _output.Unindent();
            switch (s.Type)
            {
                case ILStructureType.Loop:
                    _output.WriteLine("// end loop");
                    break;
                case ILStructureType.Try:
                    _output.WriteLine("} // end .try");
                    break;
                case ILStructureType.Handler:
                    _output.WriteLine("} // end handler");
                    break;
                case ILStructureType.Filter:
                    _output.WriteLine("} // end filter");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
