using System;
using System.IO;
using System.Text;
using RoslynPad.Annotations;

namespace RoslynPad.Utilities
{
    internal class DelegatingTextWriter : TextWriter
    {
        private readonly Action<string> _onLineWritten;
        private readonly StringBuilder _builder;

        public override Encoding Encoding => Encoding.UTF8;

        public DelegatingTextWriter([NotNull] Action<string> onLineWritten)
        {
            _onLineWritten = onLineWritten;
            _builder = new StringBuilder();
            CoreNewLine = new[] { '\n' };
        }

        public override void Write(char value)
        {
            if (value == '\n')
            {
                _onLineWritten(_builder.ToString());
                _builder.Clear();
            }
            else
            {
                _builder.Append(value);
            }
        }
    }
}