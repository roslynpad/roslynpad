using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Xunit;

namespace RoslynPad.Editor.Windows.Test
{
    public class SearchReplacePanelTests
    {
        [WpfTheory]
        [InlineData("one two two three", "two", 17, 5)]
        [InlineData("one two two three", "two", 4, 5)]
        [InlineData("one two two three", "two", 5, 9)]
        public void FindNext_WithNoSelection_SelectsExpectedMatch(string documentText, string searchPattern, int caretOffset, int expectedSelectionStartColumn)
        {
            var textArea = new TextArea { Document = new TextDocument(documentText) };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = searchPattern;

            textArea.ClearSelection();
            textArea.Caret.Offset = caretOffset;

            searchReplacePanel.FindNext();

            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(expectedSelectionStartColumn, textArea.Selection.StartPosition.Column);
            Assert.Equal(searchPattern.Length, textArea.Selection.Length);
        }

        [WpfFact]
        public void FindNext_WithMatchSelectedAndCaretAtStartOfMatch_SelectsNextMatch()
        {
            var textArea = new TextArea { Document = new TextDocument("one two two three") };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = "two";

            textArea.Selection = Selection.Create(textArea, 4, 7);
            textArea.Caret.Offset = 4;

            searchReplacePanel.FindNext();

            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(9, textArea.Selection.StartPosition.Column);
            Assert.Equal(3, textArea.Selection.Length);
        }

        [WpfTheory]
        [InlineData("one two two three", "two", 11, 9)]
        [InlineData("one two two three", "two", 10, 5)]
        [InlineData("one two two three", "two", 0, 9)]
        public void FindPrevious_WithNoSelection_SelectsExpectedMatch(string documentText, string searchPattern, int caretOffset, int expectedSelectionStartColumn)
        {
            var textArea = new TextArea { Document = new TextDocument(documentText) };

            var searchReplacePanel = SearchReplacePanel.Install(textArea);

            searchReplacePanel.Open();
            searchReplacePanel.SearchPattern = searchPattern;

            textArea.ClearSelection();
            textArea.Caret.Offset = caretOffset;

            searchReplacePanel.FindPrevious();

            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(expectedSelectionStartColumn, textArea.Selection.StartPosition.Column);
            Assert.Equal(searchPattern.Length, textArea.Selection.Length);
        }

        [WpfFact]
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

            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(5, textArea.Selection.StartPosition.Column);
            Assert.Equal(3, textArea.Selection.Length);
        }

        [WpfFact]
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

            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(5, textArea.Selection.StartPosition.Column);
            Assert.Equal(3, textArea.Selection.Length);
        }

        [WpfFact]
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

            Assert.Equal("one 2 three two", textArea.Document.Text);
            Assert.Equal(1, textArea.Selection.StartPosition.Line);
            Assert.Equal(13, textArea.Selection.StartPosition.Column);
            Assert.Equal(3, textArea.Selection.Length);
        }

        [WpfFact]
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

            Assert.Equal("one twotwo three", textArea.Document.Text);
        }

        [WpfFact]
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

            Assert.Equal("oonee twoo threeee", textArea.Document.Text);
        }
    }
}
