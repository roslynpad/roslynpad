//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Utilities;
    using System;
    using System.Diagnostics;
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// A service for displaying an adornment under the inner most closing brace.
    /// </summary>
    internal class BraceCompletionAdornmentService : IBraceCompletionAdornmentService
    {
        #region Private Members

        private ITrackingPoint _trackingPoint;
        private ITextView _textView;
        private IAdornmentLayer _adornmentLayer;
        private readonly IEditorFormatMap _editorFormatMap;
        private IBrush _brush;

        #endregion

        #region Constructors

        public BraceCompletionAdornmentService(ITextView textView, IEditorFormatMap editorFormatMap)
        {
            _textView = textView;
            _editorFormatMap = editorFormatMap;

            if (_textView == null) 
                throw new ArgumentNullException(nameof(textView));
            if (_editorFormatMap == null) 
                throw new ArgumentNullException(nameof(editorFormatMap));

            // Morgania: the upstream Cocoa branch used IXPlatAdornmentLayer; adornment layers
            // are exposed through IWpfTextView.GetAdornmentLayer per PLAN §4.2.
            _adornmentLayer = ((IWpfTextView)_textView).GetAdornmentLayer(PredefinedAdornmentLayers.BraceCompletion);

            SetBrush();
            RegisterEvents();
        }

        #endregion

        #region IBraceCompletionAdornmentService

        public ITrackingPoint Point
        {
            get
            {
                return _trackingPoint;
            }

            set
            {
                if (_trackingPoint != value)
                {
                    // always remove the old adornment first
                    if (_trackingPoint != null)
                    {
                        _adornmentLayer.RemoveAllAdornments();
                    }

                    _trackingPoint = value;

                    if (_trackingPoint != null)
                    {
                        RenderAdornment();
                    }
                }
            }
        }

        #endregion

        #region Private Helpers

        private void RegisterEvents()
        {
            _textView.Closed += TextView_Closed;
            _textView.LayoutChanged += TextView_LayoutChanged;
            _editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
        }

        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (e.ChangedItems.Contains(BraceCompletionFormat.FormatName))
            {
                SetBrush();
            }
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            UnregisterEvents();
        }

        private void UnregisterEvents()
        {
            _textView.Closed -= TextView_Closed;
            _textView.LayoutChanged -= TextView_LayoutChanged;
            _editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
        }

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_trackingPoint != null && _brush != null && _adornmentLayer.IsEmpty)
            {
                RenderAdornment();
            }
        }

        // Draw the adornment
        private void RenderAdornment()
        {
            Debug.Assert(_adornmentLayer.IsEmpty, "An adornment already exists");

            if (_trackingPoint != null && _brush != null && !_textView.IsClosed && _textView.TextViewLines != null)
            {
                // map up from the subject buffer
                SnapshotSpan? span = TranslatedSpan;

                // check that the span is visible
                if (span.HasValue && _textView.TextViewLines.FormattedSpan.Contains(span.Value.Start))
                {
                    // Morgania: the upstream Cocoa branch created an NSView underline; the
                    // Avalonia equivalent is a 2px Border under the brace character bounds.
                    TextBounds textBounds = _textView.TextViewLines.GetCharacterBounds(span.Value.Start);
                    var underline = new Avalonia.Controls.Border
                    {
                        Background = _brush,
                        Width = textBounds.Width,
                        Height = 2,
                    };
                    _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, underline, null);
                }
            }
        }

        // Gives the span the adornment will occupy on the view buffer.
        private SnapshotSpan? TranslatedSpan
        {
            get
            {
                SnapshotSpan? snapshotSpan = null;
                ITextSnapshot snapshot = _textView.TextSnapshot;
                SnapshotPoint? point = _trackingPoint.GetPoint(_trackingPoint.TextBuffer.CurrentSnapshot);

                // point.HasValue will be true since GetPoint always returns a point
                if (point.Value.Snapshot != snapshot)
                {
                    point = MappingPointSnapshot.MapUpToSnapshotNoTrack(snapshot, point.Value, PositionAffinity.Predecessor);
                }

                if (point.HasValue && point.Value.Position > 0)
                {
                    // The point is after the closing brace, we need to subtract 1 to get the span containing the brace.
                    snapshotSpan = new SnapshotSpan(point.Value.Subtract(1), 1);
                }

                return snapshotSpan;
            }
        }

        // Set the fill color of the adornment
        private void SetBrush()
        {
            var resourceDictionary = _editorFormatMap.GetProperties(BraceCompletionFormat.FormatName);

            _brush = null;
            if (resourceDictionary != null && resourceDictionary.ContainsKey(EditorFormatDefinition.BackgroundBrushId))
            {
                IBrush brush = resourceDictionary[EditorFormatDefinition.BackgroundBrushId] as IBrush;

                // leave the brush null if the opacity is zero
                if (brush != null && brush.Opacity > 0)
                {
                    _brush = brush;

                    // update the adornment with the brush
                    SetBrushAndRedrawAdornment();
                }
            }
            else
            {
                // default to light blue if no format was found
                Debug.Fail("Unable to get the brace completion adornment brush");
                _brush = Brushes.LightBlue;
            }
        }

        // Redraw the adornment if one exists
        private void SetBrushAndRedrawAdornment()
        {
            if (_trackingPoint != null)
            {
                _adornmentLayer.RemoveAllAdornments();
                RenderAdornment();
            }
        }

        #endregion
    }
}
