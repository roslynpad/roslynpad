//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Manages the insertion, anchor, and active points for a single caret and its associated
    /// selection.
    /// </summary>
    public struct Selection : IEquatable<Selection>
    {
        /// <summary>
        /// A static instance of a selection that is invalid and can be used to check for instantiation.
        /// </summary>
        public static readonly Selection Invalid = new Selection();

        /// <summary>
        /// Instantiates a new Selection with a zero-width extent at the provided insertion point.
        /// </summary>
        /// <param name="insertionPoint">The location where a caret should be rendered and edits performed.</param>
        /// <param name="insertionPointAffinity">
        /// The affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        public Selection(VirtualSnapshotPoint insertionPoint, PositionAffinity insertionPointAffinity = PositionAffinity.Successor)
            : this(insertionPoint, insertionPoint, insertionPoint, insertionPointAffinity)
        { }

        /// <summary>
        /// Instantiates a new Selection with a zero-width extent at the provided insertion point.
        /// </summary>
        /// <param name="insertionPoint">The location where a caret should be rendered and edits performed.</param>
        /// <param name="insertionPointAffinity">
        /// The affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        public Selection(SnapshotPoint insertionPoint, PositionAffinity insertionPointAffinity = PositionAffinity.Successor)
            : this(new VirtualSnapshotPoint(insertionPoint),
                   new VirtualSnapshotPoint(insertionPoint),
                   new VirtualSnapshotPoint(insertionPoint),
                   insertionPointAffinity)
        { }

        /// <summary>
        /// Instantiates a new Selection with the given extent. Anchor and active points are defined by isReversed, and the
        /// insertion point is located at the active point.
        /// </summary>
        /// <param name="extent">The span that the selection covers.</param>
        /// <param name="isReversed">
        /// True implies that <see cref="ActivePoint"/> comes before <see cref="AnchorPoint"/>.
        /// The <see cref="InsertionPoint"/> is set to the <see cref="ActivePoint"/>.
        /// <see cref="InsertionPointAffinity"/> is set to <see cref="PositionAffinity.Predecessor"/> when isReversed is true.
        /// <see cref="PositionAffinity.Successor"/> otherwise.
        /// </param>
        public Selection(VirtualSnapshotSpan extent, bool isReversed = false)
        {
            if (isReversed)
            {
                AnchorPoint = extent.End;
                ActivePoint = InsertionPoint = extent.Start;
                InsertionPointAffinity = PositionAffinity.Successor;
            }
            else
            {
                AnchorPoint = extent.Start;
                ActivePoint = InsertionPoint = extent.End;

                // The goal here is to keep the caret with the selection box. If we're wordwrapped, and the
                // box is at the end of a line, Predecessor will keep the caret on the previous line.
                InsertionPointAffinity = PositionAffinity.Predecessor;
            }
        }

        /// <summary>
        /// Instantiates a new Selection with the given extent. Anchor and active points are defined by isReversed, and the
        /// insertion point is located at the active point.
        /// </summary>
        /// <param name="extent">The span that the selection covers.</param>
        /// <param name="isReversed">
        /// True implies that <see cref="ActivePoint"/> comes before <see cref="AnchorPoint"/>.
        /// The <see cref="InsertionPoint"/> is set to the <see cref="ActivePoint"/>.
        /// <see cref="InsertionPointAffinity"/> is set to <see cref="PositionAffinity.Predecessor"/> when isReversed is true.
        /// <see cref="PositionAffinity.Successor"/> otherwise.
        /// </param>
        public Selection(SnapshotSpan extent, bool isReversed = false)
            : this(new VirtualSnapshotSpan(extent), isReversed)
        { }

        /// <summary>
        /// Instantiates a new Selection with the given anchor and active points, and the
        /// insertion point is located at the active point.
        /// </summary>
        /// <param name="anchorPoint">The location of the fixed selection endpoint, meaning if a user were to hold shift and click,
        /// this point would remain where it is.</param>
        /// <param name="activePoint">location of the movable selection endpoint, meaning if a user were to hold shift and click,
        /// this point would be changed to the location of the click.</param>
        public Selection(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
            : this(insertionPoint: activePoint,
                   anchorPoint: anchorPoint,
                   activePoint: activePoint,
                   insertionPointAffinity: (anchorPoint < activePoint) ? PositionAffinity.Predecessor : PositionAffinity.Successor)
        {
        }

        /// <summary>
        /// Instantiates a new Selection with the given anchor and active points, and the
        /// insertion point is located at the active point.
        /// </summary>
        /// <param name="anchorPoint">The location of the fixed selection endpoint, meaning if a user were to hold shift and click,
        /// this point would remain where it is.</param>
        /// <param name="activePoint">location of the movable selection endpoint, meaning if a user were to hold shift and click,
        /// this point would be changed to the location of the click.</param>
        public Selection(SnapshotPoint anchorPoint, SnapshotPoint activePoint)
            : this(anchorPoint: new VirtualSnapshotPoint(anchorPoint),
                   activePoint: new VirtualSnapshotPoint(activePoint))
        {
        }

        /// <summary>
        /// Instantiates a new Selection.
        /// </summary>
        /// <param name="insertionPoint">The location where a caret should be rendered and edits performed.</param>
        /// <param name="anchorPoint">The location of the fixed selection endpoint, meaning if a user were to hold shift and click,
        /// this point would remain where it is.</param>
        /// <param name="activePoint">location of the movable selection endpoint, meaning if a user were to hold shift and click,
        /// this point would be changed to the location of the click.</param>
        /// <param name="insertionPointAffinity">
        /// The affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        public Selection(VirtualSnapshotPoint insertionPoint,
                         VirtualSnapshotPoint anchorPoint,
                         VirtualSnapshotPoint activePoint,
                         PositionAffinity insertionPointAffinity = PositionAffinity.Successor)
        {
            if (insertionPoint.Position.Snapshot != anchorPoint.Position.Snapshot || insertionPoint.Position.Snapshot != activePoint.Position.Snapshot)
            {
                throw new ArgumentException("All points must be on the same snapshot.");
            }

            InsertionPoint = insertionPoint;
            AnchorPoint = anchorPoint;
            ActivePoint = activePoint;
            InsertionPointAffinity = insertionPointAffinity;
        }

        /// <summary>
        /// Instantiates a new Selection.
        /// </summary>
        /// <param name="insertionPoint">The location where a caret should be rendered and edits performed.</param>
        /// <param name="anchorPoint">The location of the fixed selection endpoint, meaning if a user were to hold shift and click,
        /// this point would remain where it is.</param>
        /// <param name="activePoint">location of the movable selection endpoint, meaning if a user were to hold shift and click,
        /// this point would be changed to the location of the click.</param>
        /// <param name="insertionPointAffinity">
        /// The affinity of the insertion point. This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </param>
        public Selection(SnapshotPoint insertionPoint,
                         SnapshotPoint anchorPoint,
                         SnapshotPoint activePoint,
                         PositionAffinity insertionPointAffinity = PositionAffinity.Successor)
            : this(new VirtualSnapshotPoint(insertionPoint),
                  new VirtualSnapshotPoint(anchorPoint),
                  new VirtualSnapshotPoint(activePoint),
                  insertionPointAffinity)
        { }

        /// <summary>
        /// Gets whether this selection contains meaningful data.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this != Invalid && this.InsertionPoint.Position.Snapshot != null;
            }
        }

        /// <summary>
        /// Gets the location where a caret should be rendered and edits performed.
        /// </summary>
        public VirtualSnapshotPoint InsertionPoint { get; }

        /// <summary>
        /// Gets the location of the fixed selection endpoint, meaning if a user were to hold shift and click,
        /// this point would remain where it is. If this is an empty selection, this will be at the
        /// <see cref="InsertionPoint"/>.
        /// </summary>
        public VirtualSnapshotPoint AnchorPoint { get; }

        /// <summary>
        /// Gets the location of the movable selection endpoint, meaning if a user were to hold shift and click,
        /// this point would be changed to the location of the click. If this is an empty selection, this will be at the
        /// <see cref="InsertionPoint"/>.
        /// </summary>
        public VirtualSnapshotPoint ActivePoint { get; }

        /// <summary>
        /// Gets the affinity of the insertion point.
        /// This is used in places like word-wrap where one buffer position can represent both the
        /// end of one line and the beginning of the next.
        /// </summary>
        public PositionAffinity InsertionPointAffinity { get; }

        /// <summary>
        /// True if <see cref="AnchorPoint"/> is later in the document than <see cref="ActivePoint"/>. False otherwise.
        /// </summary>
        public bool IsReversed
        {
            get
            {
                return ActivePoint < AnchorPoint;
            }
        }

        /// <summary>
        /// True if <see cref="AnchorPoint"/> equals <see cref="ActivePoint"/>. False otherwise.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return ActivePoint == AnchorPoint;
            }
        }

        /// <summary>
        /// Returns the smaller of <see cref="ActivePoint"/> and <see cref="AnchorPoint"/>.
        /// </summary>
        public VirtualSnapshotPoint Start
        {
            get
            {
                return IsReversed ? ActivePoint : AnchorPoint;
            }
        }

        /// <summary>
        /// Returns the larger of <see cref="ActivePoint"/> and <see cref="AnchorPoint"/>.
        /// </summary>
        public VirtualSnapshotPoint End
        {
            get
            {
                return IsReversed ? AnchorPoint : ActivePoint;
            }
        }

        /// <summary>
        /// Returns the span from <see cref="Start"/> to <see cref="End"/>.
        /// </summary>
        public VirtualSnapshotSpan Extent
        {
            get
            {
                return new VirtualSnapshotSpan(Start, End);
            }
        }

        public override int GetHashCode()
        {
            // We are fortunate enough to have 3 interesting points here. If you xor an even number of snapshot point hashcodes
            // together, the snapshot component gets cancelled out.

            // However, the common case is that ActivePoint and InsertionPoint are exactly equal, so we need to do something to change that.
            // Invert the bytes in InsertionPoint.GetHashCode().
            var insertionHash = (uint)InsertionPoint.GetHashCode();
            insertionHash = (((0x0000FFFF & insertionHash) << 16) | ((0xFFFF0000 & insertionHash) >> 16));

            int pointHashes = AnchorPoint.GetHashCode() ^ ActivePoint.GetHashCode() ^ (int)insertionHash;

            // InsertionPointAffinity.GetHashCode() returns either 0 or 1 which can get stomped on by the rest of the hash codes.
            // Generate more interesting hash code values below:
            int affinityHash = InsertionPointAffinity == PositionAffinity.Predecessor
                ? affinityHash = 04122013
                : affinityHash = 10172014;

            return pointHashes ^ affinityHash;
        }

        public override bool Equals(object obj)
        {
            return obj is Selection && Equals((Selection)obj);
        }

        public bool Equals(Selection other)
        {
            return this.ActivePoint == other.ActivePoint
                && this.AnchorPoint == other.AnchorPoint
                && this.InsertionPoint == other.InsertionPoint
                && this.InsertionPointAffinity == other.InsertionPointAffinity;
        }

        public static bool operator ==(Selection left, Selection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Selection left, Selection right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Ins:{InsertionPoint} Anc:{AnchorPoint} Act:{ActivePoint} Aff:{InsertionPointAffinity}";
        }
    }
}
