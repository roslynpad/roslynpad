//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using Microsoft.VisualStudio.Text.Editor;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides outlining functionality.
    /// </summary>
    /// <remarks>
    /// <para>Any methods that take <see cref="SnapshotSpan"/> objects are spans of the
    /// edit buffer in the view model of a view. This buffer can also be retrieved from
    /// the TextBuffer property of an <see cref="ITextView"/>.</para>
    /// <para>This outlining manager is provided by <see cref="IOutliningManagerService"/>.</para>
    /// </remarks>
    public interface IOutliningManager : IDisposable
    {
        /// <summary>
        /// Gets all the collapsed regions that intersect the given span.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <returns>A sorted sequence of collapsed regions.</returns>
        IEnumerable<ICollapsed> GetCollapsedRegions(SnapshotSpan span);

        /// <summary>
        /// Gets all the collapsed regions that intersect the given span.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="exposedRegionsOnly">If <c>true</c>, this returns only top-level regions (regions that aren't inside another collapsed region).</param>
        /// <returns>A sorted sequence of collapsed regions.</returns>
        IEnumerable<ICollapsed> GetCollapsedRegions(SnapshotSpan span, bool exposedRegionsOnly);

        /// <summary>
        /// Gets all the collapsed regions that intersect the given collection of spans.
        /// </summary>
        /// <param name="spans">The collection of spans.</param>
        /// <returns>A sorted sequence of collapsed regions.</returns>
        IEnumerable<ICollapsed> GetCollapsedRegions(NormalizedSnapshotSpanCollection spans);

        /// <summary>
        /// Gets all the collapsed regions that intersect the given collection of spans.
        /// </summary>
        /// <param name="spans">The collection of spans.</param>
        /// <param name="exposedRegionsOnly">If <c>true</c>, this returns only top-level regions (regions that aren't inside another collapsed region).</param>
        /// <returns>A sorted sequence of collapsed regions.</returns>
        IEnumerable<ICollapsed> GetCollapsedRegions(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly);

        /// <summary>
        /// Gets all the regions that intersect the given span, whether or not they are collapsed.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <returns>A sorted sequence of all intersecting collapsible regions.</returns>
        IEnumerable<ICollapsible> GetAllRegions(SnapshotSpan span);

        /// <summary>
        /// Gets all the regions that intersect the given span, whether or not they are collapsed.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="exposedRegionsOnly">If <c>true</c>, this returns only top-level regions (regions that aren't inside another collapsed region).</param>
        /// <returns>A sorted sequence of all intersecting collapsible regions.</returns>
        IEnumerable<ICollapsible> GetAllRegions(SnapshotSpan span, bool exposedRegionsOnly);

        /// <summary>
        /// Gets all the regions that intersect the given collection of spans, whether or not they are collapsed.
        /// </summary>
        /// <param name="spans">The collection of spans.</param>
        /// <returns>A sorted sequence of all intersecting collapsible regions.</returns>
        IEnumerable<ICollapsible> GetAllRegions(NormalizedSnapshotSpanCollection spans);

        /// <summary>
        /// Gets all the regions that intersect the given collection of spans, whether or not they are collapsed.
        /// </summary>
        /// <param name="spans">The collection of spans.</param>
        /// <param name="exposedRegionsOnly">If <c>true</c>, this returns only top-level regions (regions that aren't inside another collapsed region).</param>
        /// <returns>A sorted sequence of all intersecting collapsible regions.</returns>
        IEnumerable<ICollapsible> GetAllRegions(NormalizedSnapshotSpanCollection spans, bool exposedRegionsOnly);

        /// <summary>
        /// Occurs when the set of <see cref="ICollapsible"/> regions on the corresponding elision buffer changes.
        /// </summary>
        /// <remarks>Not raised when the collapsed state of any <see cref="ICollapsible"/> changes.</remarks>
        event EventHandler<RegionsChangedEventArgs> RegionsChanged;

        /// <summary>
        /// Occurs when an <see cref="ICollapsed"/> region is expanded.
        /// </summary>
        /// <remarks>This event is not raised when the set of <see cref="ICollapsible" /> regions on the corresponding
        /// elision buffer changes.</remarks>
        event EventHandler<RegionsExpandedEventArgs> RegionsExpanded;

        /// <summary>
        /// Occurs when an <see cref="ICollapsible"/> region is collapsed.
        /// </summary>
        /// <remarks>Not raised when the set of <see cref="ICollapsible" /> regions on the corresponding
        /// elision buffer changes.</remarks>
        event EventHandler<RegionsCollapsedEventArgs> RegionsCollapsed;

        /// <summary>
        /// Occurs when outlining has been enabled or disabled.
        /// </summary>
        event EventHandler<OutliningEnabledEventArgs> OutliningEnabledChanged;

        #region Collapsing and Expanding

        /// <summary>
        /// Expands the collapsible span.
        /// </summary>
        /// <returns>The newly-expanded span.</returns>
        ICollapsible Expand(ICollapsed collapsible);

        /// <summary>
        /// Tries to collapse a given region.
        /// </summary>
        /// <returns>The newly collapsed span if successful, otherwise null.</returns>
        /// <remarks>
        /// There are two cases in which this method can fail to collapse the region:
        /// <para>The region is already collapsed.</para>
        /// <para>The region is partially obscured because another collapsed region partially covers it.</para>
        /// </remarks>
        ICollapsed TryCollapse(ICollapsible collapsible);

        /// <summary>
        /// Collapses all regions that match the specified predicate.
        /// </summary>
        /// <param name="span">The regions that intersect this span.</param>
        /// <param name="match">The predicate to match.</param>
        /// <returns>The newly-collapsed regions.</returns>
        /// <remarks>
        /// The <paramref name="match"/> predicate may be passed regions that cannot actually be collapsed, due
        /// to the region being partially obscured by another already collapsed region (either pre-existing or collapsed
        /// in an earlier call to the predicate).  The elements of the returned enumeration do accurately track
        /// the regions that were collapsed, so they may differ from the elements for which the predicate returned <c>true</c>.
        /// </remarks>
        IEnumerable<ICollapsed> CollapseAll(SnapshotSpan span, Predicate<ICollapsible> match);
        
        /// <summary>
        /// Expands all the regions that match the specified predicate.
        /// </summary>
        /// <param name="match">The predicate to match.</param>
        /// <param name="span">The regions that intersect this span.</param>
        /// <returns>The newly-expanded regions.</returns>
        IEnumerable<ICollapsible> ExpandAll(SnapshotSpan span, Predicate<ICollapsed> match);

        #endregion

        /// <summary>
        /// Determines whether outlining is enabled.
        /// </summary>
        bool Enabled { get; set; }
    }
}
