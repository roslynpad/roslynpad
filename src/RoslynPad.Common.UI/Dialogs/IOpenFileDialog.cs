using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoslynPad.Annotations;

namespace RoslynPad.UI
{
    public interface IOpenFileDialog
    {
        bool AllowMultiple { get; set; }

        FileDialogFilter Filter { set; }

        string InitialDirectory { get; set; }

        string FileName { get; set; }

        Task<string[]?> ShowAsync();
    }

    public class FileDialogFilter
    {
        public FileDialogFilter([NotNull] string header, [NotNull] params string[] extensions)
            : this(header, (IList<string>)extensions)
        {
        }

        public FileDialogFilter([NotNull] string header, [NotNull] IList<string> extensions)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Extensions = extensions ?? throw new ArgumentNullException(nameof(extensions));
        }

        public string Header { get; }

        public IList<string> Extensions { get; }

        public override string ToString() => 
            $"{Header}|{string.Join(";", Extensions.Select(e => "*." + e))}";
    }
}