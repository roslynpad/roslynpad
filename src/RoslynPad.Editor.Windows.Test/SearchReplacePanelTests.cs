using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using NUnit.Framework;
using System.Threading;

namespace RoslynPad.Editor.Windows.Test
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class SearchReplacePanelTests
    {
        [TestCase("one two two three", "two", 4, 5)]
        [TestCase("one two two three", "two", 5, 9)]
        [TestCase("one two two three", "two", 17, 5)]
        public void FindNext_WithNoSelection_SelectsExpectedMatch(string documentText, string searchPattern, int caretOffset, int expectedSelectionStartColumn)
        {
            var textArea = new TextArea { Document = new TextDocument(documentText) };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = searchPattern;

            textArea.ClearSelection();
            textArea.Caret.Offset = caretOffset;

            searchReplacePanel.FindNext();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(expectedSelectionStartColumn));
            Assert.That(textArea.Selection.Length, Is.EqualTo(searchPattern.Length));
        }

        [Test]
        public void FindNext_WithMatchSelectedAndCaretAtStartOfMatch_SelectsNextMatch()
        {
            var textArea = new TextArea { Document = new TextDocument("one two two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";

            textArea.Selection = Selection.Create(textArea, 4, 7);
            textArea.Caret.Offset = 4;

            searchReplacePanel.FindNext();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(9));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }

        [TestCase("one two two three", "two", 11, 9)]
        [TestCase("one two two three", "two", 10, 5)]
        [TestCase("one two two three", "two", 0, 9)]
        public void FindPrevious_WithNoSelection_SelectsExpectedMatch(string documentText, string searchPattern, int caretOffset, int expectedSelectionStartColumn)
        {
            var textArea = new TextArea { Document = new TextDocument(documentText) };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = searchPattern;

            textArea.ClearSelection();
            textArea.Caret.Offset = caretOffset;

            searchReplacePanel.FindPrevious();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(expectedSelectionStartColumn));
            Assert.That(textArea.Selection.Length, Is.EqualTo(searchPattern.Length));
        }

        [Test]
        public void FindPrevious_WithMatchSelectedAndCaretAtEndOfMatch_SelectsPreviousMatch()
        {
            var textArea = new TextArea { Document = new TextDocument("one two two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";
            searchReplacePanel.FindNext();

            textArea.Selection = Selection.Create(textArea, 8, 11);
            textArea.Caret.Offset = 11;

            searchReplacePanel.FindPrevious();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(5));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }

        [Test]
        public void ReplaceNext_WithNoSelection_SelectsNextMatch()
        {
            var textArea = new TextArea { Document = new TextDocument("one two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";
            searchReplacePanel.IsReplaceMode = true;

            textArea.ClearSelection();
            textArea.Caret.Offset = 0;

            searchReplacePanel.ReplaceNext();

            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(5));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }

        [Test]
        public void ReplaceNext_WithMatchSelected_ReplacesMatchAndSelectsNextMatch()
        {
            var textArea = new TextArea { Document = new TextDocument("one two three two") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";
            searchReplacePanel.ReplacePattern = "2";
            searchReplacePanel.IsReplaceMode = true;

            textArea.Selection = Selection.Create(textArea, 4, 7);
            textArea.Caret.Offset = 4;

            searchReplacePanel.ReplaceNext();

            Assert.That(textArea.Document.Text, Is.EqualTo("one 2 three two"));
            Assert.That(textArea.Selection.StartPosition.Line, Is.EqualTo(1));
            Assert.That(textArea.Selection.StartPosition.Column, Is.EqualTo(13));
            Assert.That(textArea.Selection.Length, Is.EqualTo(3));
        }

        [Test]
        public void ReplaceNext_WithMatchSelectedAndUsingRegularExpression_ReplacesMatchWithRegexSubstitution()
        {
            var textArea = new TextArea { Document = new TextDocument("one two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "(two)";
            searchReplacePanel.ReplacePattern = "$1$1";
            searchReplacePanel.IsReplaceMode = true;
            searchReplacePanel.UseRegex = true;

            textArea.Selection = Selection.Create(textArea, 4, 7);
            textArea.Caret.Offset = 4;

            searchReplacePanel.ReplaceNext();

            Assert.That(textArea.Document.Text, Is.EqualTo("one twotwo three"));
        }

        [Test]
        public void ReplaceAll_UsingRegularExpressions_ReplacesAllMatchesWithRegexSubstitution()
        {
            var textArea = new TextArea { Document = new TextDocument("one two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "([aeiou])";
            searchReplacePanel.ReplacePattern = "$1$1";
            searchReplacePanel.IsReplaceMode = true;
            searchReplacePanel.UseRegex = true;

            searchReplacePanel.ReplaceAll();

            Assert.That(textArea.Document.Text, Is.EqualTo("oonee twoo threeee"));
        }
    }
}
