using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    internal class SelectionUIProperties : AbstractSelectionPresentationProperties
    {
        private MultiSelectionBrokerFactory _factory;
        private MultiSelectionBroker _broker;
        private IEditorOptions _options;
        private SelectionTransformer _transformer;

        public SelectionUIProperties(MultiSelectionBrokerFactory factory, MultiSelectionBroker broker, SelectionTransformer transformer)
        {
            _factory = factory;
            _broker = broker;
            _options = _factory.EditorOptionsFactoryService.GetOptions(_broker.TextView);
            _transformer = transformer;
        }

        internal void SetPreferredXCoordinate(double value)
        {
            PreferredXCoordinate = value;
        }

        internal void SetPreferredYCoordinate(double value)
        {
            PreferredYCoordinate = value;
        }

        public override bool IsOverwriteMode
        {
            get
            {
                //Perf: ContainingTextViewLine has the costly side effects. Try to do every short circut possible to return early before using it.
                if ((!_options.IsOverwriteModeEnabled())
                    || _transformer.Selection.InsertionPoint.IsInVirtualSpace
                    || (!_transformer.Selection.IsEmpty))
                {
                    return false;
                }

                // Ok, we know overwrite mode is globally on, we don't have a selection, and we're not in virtual space.
                // Now the only other check is 'are we at the end of the current line?'
                return this.ContainingTextViewLine.End.Position != _transformer.Selection.InsertionPoint.Position.Position;
            }
        }

        public override TextBounds CaretBounds
        {
            get
            {
                var width = this.CaretWidth;
                var line = this.ContainingTextViewLine;
                double left;

                if (this.IsOverwriteMode)
                {
                    var charBounds = this.ContainingTextViewLine.GetExtendedCharacterBounds(_transformer.Selection.InsertionPoint);
                    left = charBounds.Left;
                }
                else
                {
                    left = GetXCoordinateFromVirtualBufferPosition(line, _transformer.Selection.InsertionPoint);
                }
                return new TextBounds(left, line.Top, width, line.Height, line.TextTop, line.TextHeight);
            }
        }

        public override bool TryGetContainingTextViewLine(out ITextViewLine line)
        {
            line = null;
            if (_broker.TextView.InLayout || _broker.TextView.IsClosed)
            {
                return false;
            }

            // There are cases where people implement ITextView without ITextView2, so I'm doing a best effort here
            // checking InLayout and IsClosed manually rather than depending on ITextView2.TryGetTextViewLineContainingBufferPosition below.
            // The try..catch is then just paranoia to avoid throwing from a TryGet* method at all costs.
            try
            {
                var bufferPosition = _transformer.Selection.InsertionPoint.Position;

                // Problematic, though it may be, some callers like to check this during a layout, which means our
                // snapshot isn't always reliable. If this method comes up in a crash, look through the callstack for someone dispatching
                // a call without looking to see if the view is in the layout.

                ITextViewLine textLine = _broker.TextView.GetTextViewLineContainingBufferPosition(bufferPosition);

                line = _broker.TextView.GetTextViewLineContainingBufferPosition(bufferPosition);

                if ((_transformer.Selection.InsertionPointAffinity == PositionAffinity.Predecessor) && (line.Start == bufferPosition) &&
                    (_broker.TextView.TextSnapshot.GetLineFromPosition(bufferPosition).Start != bufferPosition))
                {
                    //The desired location has precedessor affinity at the start of a word wrapped line, so we
                    //really want the line before this one.
                    line = _broker.TextView.GetTextViewLineContainingBufferPosition(bufferPosition - 1);
                }

                return line != null;
            }
            catch (Exception ex)
            {
                _factory.GuardedOperations.HandleException(this, ex);
                line = null;
                return false;
            }
        }

        public override ITextViewLine ContainingTextViewLine
        {
            get
            {
                // Problematic, though it may be, some callers like to check this during a layout, which means our
                // snapshot isn't always reliable. If this method comes up in a crash, look through the callstack for someone dispatching
                // a call without looking to see if the view is in the layout.
                //
                // The property to check is ITextView.InLayout

                if (TryGetContainingTextViewLine(out var line))
                {
                    return line;
                }
                else
                {
                    try
                    {
                        throw new InvalidOperationException("Unable to get TextViewLine containing insertion point.");
                    }
                    catch (InvalidOperationException ex) when (LogException(ex))
                    {
                        // This catch block will never be reached because LogException always returns false.
                        return null;
                    }
                }
            }
        }

        private bool LogException(Exception ex)
        {
            // Ok, this is weird. What we are doing here is using guarded operations to log errors to ActivityLogs and Telemetry,
            // but we really have to throw here because by the time you get to this state there's no graceful recovery.
            _factory.GuardedOperations.HandleException(this, ex);
            return false;
        }

        public double CaretWidth
        {
            get
            {
                if (this.IsOverwriteMode)
                {
                    var bounds = this.ContainingTextViewLine.GetExtendedCharacterBounds(_transformer.Selection.InsertionPoint);
                    return bounds.Width;
                }
                else
                {
                    return (double)_options.GetOptionValue(DefaultTextViewOptions.CaretWidthId);
                }
            }
        }

        public override bool IsWithinViewport
        {
            get
            {
                // make sure the caret is on a line that's visible and that the caret is within the visual boundaries of the view
                return (((ContainingTextViewLine.VisibilityState == VisibilityState.FullyVisible)
                    && CaretBounds.Left >= _broker.TextView.ViewportLeft) && (CaretBounds.Right <= _broker.TextView.ViewportRight));
            }
        }

        /// <summary>
        /// Get the caret x coordinate for a virtual buffer position.
        /// </summary>
        /// <remarks>
        /// The x coordinate is always on the trailing edge of the previous character,
        /// *unless* the supplied buffer position is the first character on the line or
        /// is in virtual space.
        /// </remarks>
        internal static double GetXCoordinateFromVirtualBufferPosition(ITextViewLine textLine, VirtualSnapshotPoint bufferPosition)
        {
            return (bufferPosition.IsInVirtualSpace || bufferPosition.Position == textLine.Start) ?
                textLine.GetExtendedCharacterBounds(bufferPosition).Leading :
                textLine.GetExtendedCharacterBounds(bufferPosition.Position - 1).Trailing;
        }
    }
}
