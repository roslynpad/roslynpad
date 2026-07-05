//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation of the WPF-only classification format map
//  contract (PLAN §3.3/§5.4: recreated from public documentation,
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Classification.
//  IClassificationFormatMap"; the proprietary Text.UI.Wpf binary is never
//  referenced).
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Maps <see cref="IClassificationType"/> objects to the <see cref="TextFormattingRunProperties"/>
    /// used to format text of that classification.
    /// </summary>
    public interface IClassificationFormatMap
    {
        /// <summary>
        /// Gets the merged <see cref="TextFormattingRunProperties"/> for the given classification type,
        /// combining the properties contributed by the type and its base types in priority order,
        /// merged over <see cref="DefaultTextProperties"/>.
        /// </summary>
        TextFormattingRunProperties GetTextProperties(IClassificationType classificationType);

        /// <summary>
        /// Gets the <see cref="TextFormattingRunProperties"/> explicitly associated with the given
        /// classification type, without inheritance or merging over the defaults.
        /// </summary>
        TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType);

        /// <summary>
        /// Sets the properties explicitly associated with the given classification type.
        /// </summary>
        void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Sets the explicit properties of the given classification type.
        /// </summary>
        void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Adds explicit properties for a classification type that has no associated format definition,
        /// at the lowest priority.
        /// </summary>
        void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties);

        /// <summary>
        /// Adds explicit properties for a classification type that has no associated format definition,
        /// prioritized immediately before <paramref name="priority"/>.
        /// </summary>
        void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties, IClassificationType priority);

        /// <summary>
        /// Gets the key by which the properties of the given classification type are stored in the
        /// corresponding <see cref="IEditorFormatMap"/>.
        /// </summary>
        string GetEditorFormatMapKey(IClassificationType classificationType);

        /// <summary>
        /// Gets or sets the default text properties, used for text whose classification contributes
        /// no explicit properties.
        /// </summary>
        TextFormattingRunProperties DefaultTextProperties { get; set; }

        /// <summary>
        /// Gets the classification types with explicit format definitions, ordered from lowest to
        /// highest priority.
        /// </summary>
        ReadOnlyCollection<IClassificationType> CurrentPriorityOrder { get; }

        /// <summary>
        /// Switches the priorities of two classification types in <see cref="CurrentPriorityOrder"/>.
        /// </summary>
        void SwapPriorities(IClassificationType firstType, IClassificationType secondType);

        /// <summary>
        /// Begins a batch update; <see cref="ClassificationFormatMappingChanged"/> is deferred until
        /// <see cref="EndBatchUpdate"/>.
        /// </summary>
        void BeginBatchUpdate();

        /// <summary>
        /// Ends a batch update, raising <see cref="ClassificationFormatMappingChanged"/> if anything changed.
        /// </summary>
        void EndBatchUpdate();

        /// <summary>
        /// Determines whether the map is in the middle of a batch update.
        /// </summary>
        bool IsInBatchUpdate { get; }

        /// <summary>
        /// Occurs when the mapping from classification types to formatting properties changes.
        /// </summary>
        event EventHandler<EventArgs> ClassificationFormatMappingChanged;
    }
}
