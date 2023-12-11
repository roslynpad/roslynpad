using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad.UI
{
    public interface IOpenDocumentViewModel
    {
        DocumentViewModel? Document { get; }
        DocumentId DocumentId { get; }
        void Close();
        string Title { get; }
        bool IsDirty { get; }

        Task<SaveResult> SaveAsync(bool promptSave);
        Task AutoSaveAsync();
    }
}
