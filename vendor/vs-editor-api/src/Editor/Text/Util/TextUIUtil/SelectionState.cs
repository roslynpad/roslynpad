//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    public class SelectionState
    {
        private readonly SingleSelection[] _selections;
        private readonly SingleSelection _primary;
        private bool _isBox;

        public SelectionState(ITextView view)
        {
            var selectionBroker = view.GetMultiSelectionBroker();
            var map = SelectionState.EditToDataMap(view);

            if (selectionBroker.IsBoxSelection)
            {
                _primary = new SingleSelection(map, selectionBroker.BoxSelection);
                _isBox = true;
            }
            else
            {
                if (selectionBroker.HasMultipleSelections)
                {
                    var selections = selectionBroker.AllSelections;
                    _selections = new SingleSelection[selections.Count];
                    for (int i = 0; (i < _selections.Length); ++i)
                    {
                        _selections[i] = new SingleSelection(map, selections[i]);
                    }
                }

                _primary = new SingleSelection(map, selectionBroker.PrimarySelection);
            }
        }

        public void Restore(ITextView view)
        {
            var selectionBroker = view.GetMultiSelectionBroker();
            var map = SelectionState.EditToDataMap(view);

            if (_isBox)
            {
                selectionBroker.SetBoxSelection(_primary.Rehydrate(map, selectionBroker.CurrentSnapshot));
            }
            else
            {
                Selection[] rehydradedSelections = null;
                if (_selections != null)
                {
                    rehydradedSelections = new Selection[_selections.Length];
                    for (int i = 0; (i < _selections.Length); ++i)
                    {
                        rehydradedSelections[i] = _selections[i].Rehydrate(map, selectionBroker.CurrentSnapshot);
                    }
                }

                selectionBroker.SetSelectionRange(rehydradedSelections, _primary.Rehydrate(map, selectionBroker.CurrentSnapshot));
            }
        }

        public bool Matches(SelectionState other)
        {
            if ((_isBox == other._isBox) && _primary.Matches(other._primary))
            {
                if (_selections != null)
                {
                    if ((other._selections != null) && (_selections.Length == other._selections.Length))
                    {
                        for (int i = 0; (i < _selections.Length); ++i)
                        {
                            if (!_selections[i].Matches(other._selections[i]))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
                else if (other._selections == null)
                    return true;
            }

            return false;
        }

        public static IMapEditToData EditToDataMap(ITextView view)
        {
            return (view.TextViewModel.EditBuffer != view.TextViewModel.DataBuffer) && view.Properties.TryGetProperty(typeof(IMapEditToData), out IMapEditToData map) && (map != null)
                   ? map
                   : VacuousMapToEdit.Identity;
        }

        struct SingleSelection
        {
            public readonly VirtualPoint Anchor;
            public readonly VirtualPoint Active;
            public readonly VirtualPoint Insertion;
            public readonly PositionAffinity Affinity;

            public SingleSelection(IMapEditToData map, Selection selection)
            {
                this.Anchor = new VirtualPoint(map, selection.AnchorPoint);
                this.Active = new VirtualPoint(map, selection.ActivePoint);
                this.Insertion = new VirtualPoint(map, selection.InsertionPoint);
                this.Affinity = selection.InsertionPointAffinity;
            }

            public Selection Rehydrate(IMapEditToData map, ITextSnapshot snapshot)
            {
                return new Selection(this.Insertion.Rehydrate(map, snapshot),
                                     this.Anchor.Rehydrate(map, snapshot),
                                     this.Active.Rehydrate(map, snapshot),
                                     this.Affinity);
            }

            public bool Matches(SingleSelection other)
            {
                return this.Anchor.Matches(other.Anchor) && this.Active.Matches(other.Active) && this.Insertion.Matches(other.Insertion) && (this.Affinity == other.Affinity);
            }
        }

        struct VirtualPoint
        {
            public readonly int Position;
            public readonly int VirtualSpaces;
            public VirtualPoint(IMapEditToData map, VirtualSnapshotPoint point) { this.Position = map.MapEditToData(point.Position); this.VirtualSpaces = point.VirtualSpaces; }

            public VirtualSnapshotPoint Rehydrate(IMapEditToData map, ITextSnapshot snapshot) => new VirtualSnapshotPoint(new SnapshotPoint(snapshot, map.MapDataToEdit(this.Position)), this.VirtualSpaces);

            public bool Matches(VirtualPoint other) => (this.Position == other.Position) && (this.VirtualSpaces == other.VirtualSpaces);
        }

        private class VacuousMapToEdit : IMapEditToData
        {
            public static readonly IMapEditToData Identity = new VacuousMapToEdit();

            public int MapDataToEdit(int dataPoint) => dataPoint;
            public int MapEditToData(int editPoint) => editPoint;
        }
    }
}
