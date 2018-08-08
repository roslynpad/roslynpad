using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using NUnit.Framework;
using System.Threading;

namespace RoslynPad.Editor.Windows.Test
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class SearchReplacePanelTests
    {
        [Test]
        public void ReplaceNext_WhenNoResultSelected_SelectsNextResult()
        {
            var textArea = new TextArea { Document = new TextDocument("one two three four") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";
            searchReplacePanel.IsReplaceMode = true;

            textArea.ClearSelection();

            searchReplacePanel.ReplaceNext();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(5));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }

        [Test]
        public void ReplaceNext_WhenResultSelected_ReplacesResultAndSelectsNext()
        {
            var textArea = new TextArea { Document = new TextDocument("one two one two") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";
            searchReplacePanel.ReplacePattern = "2";
            searchReplacePanel.IsReplaceMode = true;

            searchReplacePanel.ReplaceNext();

            Assert.That(textArea.Document.Text, Is.EqualTo("one 2 one two"));
            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(11));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }
    }
}
