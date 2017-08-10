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
using System.IO;

namespace RoslynPad.Hosting.ILDecompiler
{
	internal interface ITextOutput
	{
		void Indent();
		void Unindent();
		void Write(char ch);
		void Write(string text);
		void WriteLine();
		void WriteDefinition(string text, object definition, bool isLocal = true);
		void WriteReference(string text, object reference, bool isLocal = false);

        void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false);
		void MarkFoldEnd();
	}
	
	internal static class TextOutputExtensions
	{
		public static void Write(this ITextOutput output, string format, params object[] args)
		{
			output.Write(string.Format(format, args));
		}
		
		public static void WriteLine(this ITextOutput output, string text)
		{
			output.Write(text);
			output.WriteLine();
		}
		
		public static void WriteLine(this ITextOutput output, string format, params object[] args)
		{
			output.WriteLine(string.Format(format, args));
		}
	}

    internal sealed class PlainTextOutput : ITextOutput
    {
        private readonly TextWriter _writer;
        private int _indent;
        private bool _needsIndent;

        public PlainTextOutput(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public PlainTextOutput()
        {
            _writer = new StringWriter();
        }
        
        public override string ToString()
        {
            return _writer.ToString();
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;
        }

        private void WriteIndent()
        {
            if (_needsIndent)
            {
                _needsIndent = false;
                for (var i = 0; i < _indent; i++)
                {
                    _writer.Write('\t');
                }
            }
        }

        public void Write(char ch)
        {
            WriteIndent();
            _writer.Write(ch);
        }

        public void Write(string text)
        {
            WriteIndent();
            _writer.Write(text);
        }

        public void WriteLine()
        {
            _writer.WriteLine();
            _needsIndent = true;
        }

        public void WriteDefinition(string text, object definition, bool isLocal)
        {
            Write(text);
        }

        public void WriteReference(string text, object reference, bool isLocal)
        {
            Write(text);
        }

        void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed)
        {
        }

        void ITextOutput.MarkFoldEnd()
        {
        }
    }
}
