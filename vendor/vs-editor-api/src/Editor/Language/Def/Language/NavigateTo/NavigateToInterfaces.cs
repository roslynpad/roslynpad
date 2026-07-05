using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;

namespace Microsoft.VisualStudio.Language.NavigateTo.Interfaces
{
    /// <summary>
    /// An item produced by a Navigate To (go to all) search.
    /// </summary>
    public sealed class NavigateToItem
    {
        public NavigateToItem(
            string name,
            string kind,
            string language,
            string secondarySort,
            object tag,
            PatternMatch patternMatch,
            INavigateToItemDisplayFactory displayFactory)
        {
            Name = name;
            Kind = kind;
            Language = language;
            SecondarySort = secondarySort;
            Tag = tag;
            PatternMatch = patternMatch;
            DisplayFactory = displayFactory;
        }

        public string Name { get; }

        public string Kind { get; }

        public string Language { get; }

        public string SecondarySort { get; }

        public object Tag { get; }

        public PatternMatch PatternMatch { get; }

        public INavigateToItemDisplayFactory DisplayFactory { get; }
    }

    /// <summary>
    /// Creates display objects for <see cref="NavigateToItem"/>s.
    /// </summary>
    public interface INavigateToItemDisplayFactory
    {
        INavigateToItemDisplay CreateItemDisplay(NavigateToItem item);
    }

    /// <summary>
    /// A run of text in a <see cref="DescriptionItem"/>.
    /// </summary>
    public sealed class DescriptionRun
    {
        public DescriptionRun(string text, bool bold = false, bool italic = false, bool underline = false)
        {
            Text = text;
            Bold = bold;
            Italic = italic;
            Underline = underline;
        }

        public string Text { get; }

        public bool Bold { get; }

        public bool Italic { get; }

        public bool Underline { get; }
    }

    /// <summary>
    /// A category/details pair describing an aspect of a Navigate To item.
    /// </summary>
    public sealed class DescriptionItem
    {
        public DescriptionItem(ReadOnlyCollection<DescriptionRun> category, ReadOnlyCollection<DescriptionRun> details)
        {
            Category = category;
            Details = details;
        }

        public ReadOnlyCollection<DescriptionRun> Category { get; }

        public ReadOnlyCollection<DescriptionRun> Details { get; }
    }

    /// <summary>
    /// Provides display information for a <see cref="NavigateToItem"/>.
    /// </summary>
    /// <remarks>
    /// Unlike the original Visual Studio interface, this omits the legacy
    /// <c>System.Drawing.Icon</c>-typed <c>Glyph</c> member; use
    /// <see cref="INavigateToItemDisplay3.GlyphMoniker"/> instead.
    /// </remarks>
    public interface INavigateToItemDisplay
    {
        string Name { get; }

        string AdditionalInformation { get; }

        string Description { get; }

        ReadOnlyCollection<DescriptionItem> DescriptionItems { get; }

        void NavigateTo();
    }

    public interface INavigateToItemDisplay2 : INavigateToItemDisplay
    {
        int GetProvisionalViewingStatus();

        void PreviewItem();
    }

    public interface INavigateToItemDisplay3 : INavigateToItemDisplay2
    {
        ImageMoniker GlyphMoniker { get; }

        IReadOnlyList<Span> GetNameMatchRuns(string searchValue);

        IReadOnlyList<Span> GetAdditionalInformationMatchRuns(string searchValue);
    }

    /// <summary>
    /// Options for a Navigate To search.
    /// </summary>
    public interface INavigateToOptions
    {
    }

    public interface INavigateToOptions2 : INavigateToOptions
    {
        bool SearchCurrentDocument { get; }
    }

    /// <summary>
    /// The reason a Navigate To search completed without full results.
    /// </summary>
    public enum IncompleteReason
    {
        SolutionLoading,
        Parsing,
    }

    /// <summary>
    /// Callback used by <see cref="INavigateToItemProvider"/> to report search results.
    /// </summary>
    public interface INavigateToCallback
    {
        INavigateToOptions Options { get; }

        void AddItem(NavigateToItem item);

        void ReportProgress(int current, int maximum);

        void Invalidate();

        void Done();
    }

    public interface INavigateToCallback2 : INavigateToCallback
    {
        void Done(IncompleteReason reason);
    }

    /// <summary>
    /// Filter applied to a Navigate To search.
    /// </summary>
    public interface INavigateToFilterParameters
    {
        ISet<string> Kinds { get; }
    }

    /// <summary>
    /// Performs Navigate To searches.
    /// </summary>
    public interface INavigateToItemProvider : IDisposable
    {
        void StartSearch(INavigateToCallback callback, string searchValue);

        void StopSearch();
    }

    public interface INavigateToItemProvider2 : INavigateToItemProvider
    {
        ISet<string> KindsProvided { get; }

        bool CanFilter { get; }

        void StartSearch(INavigateToCallback callback, string searchValue, INavigateToFilterParameters filter);
    }
}
