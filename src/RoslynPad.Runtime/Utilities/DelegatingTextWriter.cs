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

        public override void Flush()
        {
            if (_builder.Length > 0)
            {
                FlushInternal();
            }
        }

        public override void Write(char value)
        {
            if (value == '\n')
            {
                FlushInternal();
            }
            else
            {
                _builder.Append(value);
            }
        }

        private void FlushInternal()
        {
            _onLineWritten(_builder.ToString());
            _builder.Clear();
        }
    }
}