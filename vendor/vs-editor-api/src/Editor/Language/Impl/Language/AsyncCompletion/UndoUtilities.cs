// Taken from Roslyn's Controller_Commit.cs

using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Code taken from http://source.roslyn.io/#Microsoft.CodeAnalysis.EditorFeatures/Implementation/IntelliSense/Completion/Controller_Commit.cs
    /// </summary>
    internal static class UndoUtilities
    {
        internal static void RollbackToBeforeTypeChar(ITextSnapshot initialTextSnapshot, ITextBuffer subjectBuffer)
        {
            // Get all the versions from the initial text snapshot (before we passed the
            // commit character down) to the current snapshot we're at.
            var versions = GetVersions(initialTextSnapshot, subjectBuffer.CurrentSnapshot).ToList();

            // Un-apply the edits. 
            for (var i = versions.Count - 1; i >= 0; i--)
            {
                var version = versions[i];
                using (var textEdit = subjectBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
                {
                    foreach (var change in version.Changes)
                    {
                        textEdit.Replace(change.NewSpan, change.OldText);
                    }
                    textEdit.Apply();
                }
            }
        }

        internal static IEnumerable<ITextVersion> GetVersions(
            ITextSnapshot initialTextSnapshot, ITextSnapshot currentSnapshot)
        {
            var version = initialTextSnapshot.Version;
            while (version != null && version.VersionNumber != currentSnapshot.Version.VersionNumber)
            {
                yield return version;
                version = version.Next;
            }
        }
    }
}
