using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using RoslynPad.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad
{
    public enum CompareAction
    {
        None,
        Blank,
        Added,
        Deleted
    }
    public class CompareLine
    {
        public CompareLine(CompareAction type, int number, string content)
        {
            Type = type;
            Content = content;
            Number = number;
        }
        public CompareAction Type { get; }
        public string Content { get; }
        public int Number { get; }

    }
    public class CompareDocuemnt : List<CompareLine>
    {
        public string Title { get; set; } = "";
        public string Text
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var i in this)
                {
                    var type = i.Type;
                    if (type == CompareAction.Blank)
                        sb.AppendLine();
                    else
                        sb.AppendLine(i.Content);
                }
                return sb.ToString();
            }
        }
    }
    public class GitFileCompareViewModel : IOpenDocumentViewModel, IDisposable
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public GitFileCompareViewModel(string path, CompareDocuemnt newDoc, CompareDocuemnt oldDoc)
        {
            Path = path;
            DocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());
            NewDocument = newDoc;
            OldDocument = oldDoc;
        }
        public MainViewModel MainViewModel { get; set; }
        public DocumentViewModel? Document => null;
        public DocumentId DocumentId { get; }

        public string Title { get; set; } = "";

        public bool IsDirty => false;
        public string Path { get; set; }
        public CompareDocuemnt NewDocument { get; }
        public CompareDocuemnt OldDocument { get; }

        public Task AutoSaveAsync()
        {
            return Task.Run(() =>
            {

            });
        }

        public void Close()
        {
        }

        public Task<SaveResult> SaveAsync(bool promptSave)
        {
            return Task<SaveResult>.Run(() =>
            {
                return SaveResult.DoNotSave;
            });
        }
        public void Dispose()
        {
        }
        /// <summary>
        /// compare file between two commit
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="commitId"></param>
        /// <param name="path"></param>
        /// <param name="previousCommitId"></param>
        /// <returns></returns>
        public static GitFileCompareViewModel? CompareFile(Repository repository, string commitId, string path, string previousCommitId)
        {
            var commit = repository.Lookup<Commit>(commitId);
            var prevCommit = repository.Lookup<Commit>(previousCommitId);
            if (commit == null || prevCommit==null) return null;
            var current = commit.Tree[path];
            var previous = prevCommit.Tree[path];
            if (current == null || previous == null) return null;
            var currentBlob = current.Target as Blob;
            var previousBlob = previous.Target as Blob;
            if (currentBlob == null || previousBlob == null) return null;
            var docs = PareComparePatch(repository, currentBlob, previousBlob);
            docs.Item1.Title = string.Concat( path,previousCommitId.Substring(0,8));
            docs.Item2.Title = string.Concat( path, commitId.Substring(0,8));
            return new GitFileCompareViewModel(path, docs.Item1, docs.Item2);
        }
        /// <summary>
        /// compare file between commit and current
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="commitId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GitFileCompareViewModel? CompareFile(Repository repository, string commitId, string path)
        {
            var lastFile = repository.Lookup<Commit>(commitId).Tree[path];
            if (lastFile == null) return null;
            var current = repository.ObjectDatabase.CreateBlob(path);
            var old = lastFile.Target as Blob;
            if (old == null) return null;
            var docs = PareComparePatch(repository, current, old);
            docs.Item1.Title = path;
            docs.Item2.Title = string.Concat( path, commitId.Substring(0,8));
            return new GitFileCompareViewModel(path, docs.Item1, docs.Item2);
        }
        static CompareDocuemnt BlobToDoc(Blob blob)
        {
            CompareDocuemnt doc = new CompareDocuemnt();
            using(var stream=new StreamReader(blob.GetContentStream()))
            {
                var line = "";
                int index = 0;
                while ((line = stream.ReadLine()) != null)
                    doc.Add(new CompareLine(CompareAction.None, ++index, line));
            }
            return doc;
        }
        /// <summary>
        /// compare file with head branch
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GitFileCompareViewModel? CompareFile(Repository repository, string path)
        {
            var lastFile = repository.Head.Tip.Tree[path];
            if (lastFile == null) return null;
            var current = repository.ObjectDatabase.CreateBlob(path);
            var old = lastFile.Target as Blob;
            if (old == null) return null;
            var docs = PareComparePatch(repository, current, old);
            docs.Item1.Title = path;
            docs.Item2.Title = path + ";HEAD";
            return new GitFileCompareViewModel(path, docs.Item1, docs.Item2);
        }
        static List<string> GetLinesFrom(Blob blob)
        {
            List<string> lines = new List<string>();
            using (var stream = new StreamReader(blob.GetContentStream()))
            {
                var line = "";
                while ((line = stream.ReadLine()) != null)
                    lines.Add(line);
            }
            return lines;
        }
        static readonly string[] LineBreaks = new string[] { "\r\n", "\n" };
        static readonly string[] Prefix = new string[] { "@@" };
        static readonly char[] Seperators = new char[] { ' ', ',' };
        static Tuple<CompareDocuemnt, CompareDocuemnt> PareComparePatch(Repository repository, Blob newBlob, Blob oldBob)
        {
            var compare = repository.Diff.Compare(oldBob, newBlob);
            if (compare.LinesAdded == 0 && compare.LinesDeleted == 0)
            {
                var newDoc2 = BlobToDoc(newBlob);
                var oldDoc2 = BlobToDoc(oldBob);
                return new Tuple<CompareDocuemnt, CompareDocuemnt>(newDoc2, oldDoc2);
            }
            var patch = compare.Patch;
            //Console.WriteLine(patch);
            var lines = patch.Split(LineBreaks, StringSplitOptions.None);
            CompareDocuemnt newDoc = new CompareDocuemnt();
            CompareDocuemnt oldDoc = new CompareDocuemnt();
            var newLines = GetLinesFrom(newBlob);
            var oldLines = GetLinesFrom(oldBob);
            int newLine = 0;
            int oldLine = 0;
            int newAdd = 0;
            int oldAdd = 0;
            int newStart = 1;
            int oldStart = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                char type = lines[i][0];
                string content = lines[i].Substring(1);
                if (type == ' ')
                {
                    var diff = newAdd - oldAdd;
                    for (int j = 0; j < diff; j++)
                        oldDoc.Add(new CompareLine(CompareAction.Blank, 0, ""));
                    for (int j = 0; j < -diff; j++)
                        newDoc.Add(new CompareLine(CompareAction.Blank, 0, ""));

                    newAdd = 0;
                    oldAdd = 0;
                    newLine++;
                    newDoc.Add(new CompareLine(CompareAction.None, newLine, content));
                    oldLine++;
                    oldDoc.Add(new CompareLine(CompareAction.None, oldLine, content));
                }
                else if (type == '+')
                {
                    newAdd++;
                    newLine++;
                    newDoc.Add(new CompareLine(CompareAction.Added, newLine, content));
                }
                else if (type == '-')
                {
                    oldAdd++;
                    oldLine++;
                    oldDoc.Add(new CompareLine(CompareAction.Deleted, oldLine, content));
                }
                else if (type == '@')
                {
                    var indexLine = lines[i].Split(Prefix, StringSplitOptions.RemoveEmptyEntries)[0];
                    var splits=indexLine.Split(Seperators, StringSplitOptions.RemoveEmptyEntries);
                    var voldStart = -int.Parse(splits[0], CultureInfo.InvariantCulture);
                    var voldLength = int.Parse(splits[1], CultureInfo.InvariantCulture);
                    var vnewStart = int.Parse(splits[2], CultureInfo.InvariantCulture);
                    var vnewLength = int.Parse(splits[3], CultureInfo.InvariantCulture);
                    for(int j = oldStart; j < voldStart; j++)
                    {
                        oldLine++;
                        oldAdd++;
                        oldDoc.Add(new CompareLine(CompareAction.None, oldLine, oldLines[j-1]));
                    }
                    for(int j = newStart; j < vnewStart; j++)
                    {
                        newLine++;
                        newAdd++;
                        newDoc.Add(new CompareLine(CompareAction.None, newLine, newLines[j-1]));
                    }
                    oldStart = voldStart + voldLength;
                    newStart = vnewStart + vnewLength;
                }
            }
            for(int i = oldStart; i <= oldLines.Count; i++)
            {
                oldLine++;
                oldDoc.Add(new CompareLine(CompareAction.None, oldLine, oldLines[i - 1]));
            }
            for(int i = newStart; i <= newLines.Count; i++)
            {
                newLine++;
                newDoc.Add(new CompareLine(CompareAction.None, newLine, newLines[i - 1]));
            }
            return new Tuple<CompareDocuemnt, CompareDocuemnt>(newDoc, oldDoc);
        }
    }
}
