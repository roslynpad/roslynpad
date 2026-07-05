//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Composition;
using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods
{
    /// <summary>
    /// Extension methods for common general options.
    /// </summary>
    public static class DefaultOptionExtensions
    {
        #region Extension methods
        /// <summary>
        /// Determines whether the option to convert tabs to spaces is enabled in the specified <see cref="IEditorOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if the option is enabled, otherwise <c>false</c>.</returns>
        public static bool IsConvertTabsToSpacesEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
        }

        /// <summary>
        ///Gets the size of the tab for the specified <see cref="IEditorOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>The number of spaces of the tab size.</returns>
        public static int GetTabSize(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.TabSizeOptionId);
        }

        /// <summary>
        ///Gets the size of an indent for the specified <see cref="IEditorOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>The number of spaces of the indent size.</returns>
        public static int GetIndentSize(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.IndentSizeOptionId);
        }

        /// <summary>
        /// Determines whether to duplicate the new line character if it is already present when inserting a new line.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if the new line character should be duplicated, otherwise <c>false</c>.</returns>
        public static bool GetReplicateNewLineCharacter(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.ReplicateNewLineCharacterOptionId);
        }

        /// <summary>
        /// Gets the new line character for the specified editor options.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>A string containing the new line character or characters.</returns>
        public static string GetNewLineCharacter(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId);
        }

        /// <summary>
        /// Determines whether to trim trailing whitespace.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if trailing whitespace should be trimmed, otherwise <c>false</c>.</returns>
        public static bool GetTrimTrailingWhieSpace(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.TrimTrailingWhiteSpaceOptionId);
        }

        /// <summary>
        /// Determines whether to insert final newline.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if a final new line should be inserted, otherwise <c>false</c>.</returns>
        public static bool GetInsertFinalNewLine(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.InsertFinalNewLineOptionId);
        }

        /// <summary>
        /// Determines appearance category for tooltips originating in this view
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns>A string containing the appearance category for tooltips originating in this view.</returns>
        public static string GetTooltipAppearanceCategory(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultOptions.TooltipAppearanceCategoryOptionId);
        }

        #endregion
    }
}

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Common general options.
    /// </summary>
    public static class DefaultOptions
    {
        #region Option identifiers
        /// <summary>
        /// The default option that determines whether to convert tabs to spaces.
        /// </summary>
        public static readonly EditorOptionKey<bool> ConvertTabsToSpacesOptionId = new EditorOptionKey<bool>(ConvertTabsToSpacesOptionName);
        /// <summary>
        /// The name of the option that holds the raw coding-conventions (editorconfig) snapshot
        /// for the buffer, when provided by the host.
        /// </summary>
        public const string RawCodingConventionsSnapshotOptionName = "RawCodingConventionsSnapshot";

        /// <summary>
        /// The default option that holds the raw coding-conventions (editorconfig) snapshot for the buffer.
        /// </summary>
        public static readonly EditorOptionKey<System.Collections.Generic.IDictionary<string, object>> RawCodingConventionsSnapshotOptionId = new EditorOptionKey<System.Collections.Generic.IDictionary<string, object>>(RawCodingConventionsSnapshotOptionName);

        public const string ConvertTabsToSpacesOptionName = "Tabs/ConvertTabsToSpaces";

        /// <summary>
        /// The default option that determines size of a tab.
        /// </summary>
        /// <remarks>This option is used to determine the numerical column offset of a tab
        /// character ('\t') and, if <see cref="ConvertTabsToSpaces"/> is enabled, the number of spaces to which a tab
        /// should be converted.</remarks>
        public static readonly EditorOptionKey<int> TabSizeOptionId = new EditorOptionKey<int>(TabSizeOptionName);
        public const string TabSizeOptionName = "Tabs/TabSize";

        /// <summary>
        /// The default option that determines size of an indent.
        /// </summary>
        /// <remarks>This option is used to determine the numerical column offset of an indent level.</remarks>
        public static readonly EditorOptionKey<int> IndentSizeOptionId = new EditorOptionKey<int>(IndentSizeOptionName);
        public const string IndentSizeOptionName = "Tabs/IndentSize";

        /// <summary>
        /// The default option that determines whether to duplicate the new line character already present
        /// when inserting a new line.
        /// </summary>
        public static readonly EditorOptionKey<bool> ReplicateNewLineCharacterOptionId = new EditorOptionKey<bool>(ReplicateNewLineCharacterOptionName);
        public const string ReplicateNewLineCharacterOptionName = "ReplicateNewLineCharacter";

        /// <summary>
        /// The default option that determines the newline character or characters. 
        /// </summary>
        /// <remarks>The newline character can be a string, as in the common case of "\r\n". This setting applies
        /// when <see cref="ReplicateNewLineCharacter"/> is <c>false</c>, or when <see cref="ReplicateNewLineCharacter"/> is <c>true</c> and
        /// the text buffer is empty.</remarks>
        public static readonly EditorOptionKey<string> NewLineCharacterOptionId = new EditorOptionKey<string>(NewLineCharacterOptionName);
        public const string NewLineCharacterOptionName = "NewLineCharacter";

        /// <summary>
        /// The default option that determines the threshold for special handling of long lines.
        /// </summary>
        /// <remarks>
        /// Some operations will not operate on lines longer than this threshold.
        /// </remarks>
        public static readonly EditorOptionKey<int> LongBufferLineThresholdId = new EditorOptionKey<int>(LongBufferLineThresholdOptionName);
        public const string LongBufferLineThresholdOptionName = "LongBufferLineThreshold";

        /// <summary>
        /// The default option that determines the chunking size for long lines.
        /// </summary>
        /// <remarks>
        /// Lines longer than <see cref="LongBufferLineThreshold"/> may be considered in chunks of this size.
        /// </remarks>
        public static readonly EditorOptionKey<int> LongBufferLineChunkLengthId = new EditorOptionKey<int>(LongBufferLineChunkLengthOptionName);
        public const string LongBufferLineChunkLengthOptionName = "LongBufferLineChunkLength";

        /// <summary>
        /// The default option that determines whether to trim trailing whitespace.
        /// </summary>
        public static readonly EditorOptionKey<bool> TrimTrailingWhiteSpaceOptionId = new EditorOptionKey<bool>(TrimTrailingWhiteSpaceOptionName);
        public const string TrimTrailingWhiteSpaceOptionName = "TrimTrailingWhiteSpace";

        /// <summary>
        /// The default option that determines whether to insert final new line charcter.
        /// </summary>
        public static readonly EditorOptionKey<bool> InsertFinalNewLineOptionId = new EditorOptionKey<bool>(InsertFinalNewLineOptionName);
        public const string InsertFinalNewLineOptionName = "InsertFinalNewLine";

        /// <summary>
        /// The default option that determines appearance category for tooltips originating in this view.
        /// </summary>
        public static readonly EditorOptionKey<string> TooltipAppearanceCategoryOptionId = new EditorOptionKey<string>(TooltipAppearanceCategoryOptionName);
        public const string TooltipAppearanceCategoryOptionName = "TooltipAppearanceCategory";

        /// <summary>
        /// The default option that determines whether files, when opened, attempt to detect for a utf-8 encoding.
        /// </summary>
        public static readonly EditorOptionKey<bool> AutoDetectUtf8Id = new EditorOptionKey<bool>(AutoDetectUtf8Name);
        public const string AutoDetectUtf8Name = "AutoDetectUtf8";

        /// <summary>
        /// The default option that determines whether matching delimiters should be highlighted.
        /// </summary>
        public static readonly EditorOptionKey<bool> AutomaticDelimiterHighlightingId = new EditorOptionKey<bool>(AutomaticDelimiterHighlightingName);
        public const string AutomaticDelimiterHighlightingName = "AutomaticDelimiterHighlighting";

        /// <summary>
        /// The default option that determines whether files should follow project coding conventions.
        /// </summary>
        public static readonly EditorOptionKey<bool> FollowCodingConventionsId = new EditorOptionKey<bool>(FollowCodingConventionsName);
        public const string FollowCodingConventionsName = "FollowCodingConventions";

        /// <summary>
        /// The default option that determines the editor emulation mode.
        /// </summary>
        public static readonly EditorOptionKey<int> EditorEmulationModeId = new EditorOptionKey<int>(EditorEmulationModeName);
        public const string EditorEmulationModeName = "EditorEmulationMode";

        /// <summary>
        /// The option definition that determines maximum allowed typing latency value in milliseconds. Its value comes either
        /// from remote settings or from <see cref="UserCustomMaximumTypingLatencyOption"/> if user specifies it in
        /// Tools/Options/Text Editor/Advanced page.
        /// </summary>
        internal static readonly EditorOptionKey<int> MaximumTypingLatencyOptionId = new EditorOptionKey<int>(MaximumTypingLatencyOptionName);
        internal const string MaximumTypingLatencyOptionName = "MaximumTypingLatency";

        /// <summary>
        /// The option definition that determines user custom maximum allowed typing latency value in milliseconds. If user
        /// specifies it on Tools/Options/Text Editor/Advanced page, it becomes a source for the <see cref="MaximumTypingLatency"/> option.
        /// </summary>
        internal static readonly EditorOptionKey<int> UserCustomMaximumTypingLatencyOptionId = new EditorOptionKey<int>(UserCustomMaximumTypingLatencyOptionName);
        internal const string UserCustomMaximumTypingLatencyOptionName = "UserCustomMaximumTypingLatency";

        /// <summary>
        /// The option definition that determines whether to enable typing latency guarding.
        /// </summary>
        internal static readonly EditorOptionKey<bool> EnableTypingLatencyGuardOptionId = new EditorOptionKey<bool>(EnableTypingLatencyGuardOptionName);
        internal const string EnableTypingLatencyGuardOptionName = "EnableTypingLatencyGuard";

        /// <summary>
        /// Option that defines the fallback font for the editor.
        /// </summary>
        /// <remarks>
        /// Note that, unlike most other options, this value is only checked once at startup on <see cref="IEditorOptionsFactoryService.GlobalOptions"/>
        /// and we do not react to changes.
        /// </remarks>
        public static readonly EditorOptionKey<string> FallbackFontId = new EditorOptionKey<string>(FallbackFontName);
        public const string FallbackFontName = "FallbackFont";

        /// <summary>
        /// Option that defines when Editor should not block waiting for computation of completion items,
        /// and either use the last good computed set of completion items, or dismiss completion if no completion items were computed so far.
        /// </summary>
        public static readonly EditorOptionKey<bool> NonBlockingCompletionOptionId = new EditorOptionKey<bool>(NonBlockingCompletionOptionName);
        public const string NonBlockingCompletionOptionName = "NonBlockingCompletion";

        /// <summary>
        /// Option that defines how long Editor should block waiting for computation of completion items, in miliseconds,
        /// and either use the last good computed set of completion items, or dismiss completion if no completion items were computed so far.
        /// </summary>
        public static readonly EditorOptionKey<bool> ResponsiveCompletionOptionId = new EditorOptionKey<bool>(ResponsiveCompletionOptionName);
        public const string ResponsiveCompletionOptionName = "ResponsiveCompletion";

        /// <summary>
        /// Option that defines how long Editor should block waiting for computation of completion items, in miliseconds,
        /// and either use the last good computed set of completion items, or dismiss completion if no completion items were computed so far.
        /// </summary>
        public static readonly EditorOptionKey<int> ResponsiveCompletionThresholdOptionId = new EditorOptionKey<int>(ResponsiveCompletionThresholdOptionName);
        public const string ResponsiveCompletionThresholdOptionName = "ResponsiveCompletionThreshold";

        /// <summary>
        /// Option that keeps track of whether the responsive mode is enabled using remotely controlled feature flags.
        /// If set to false, the feature is off, despite user's choice stored in <see cref="ResponsiveCompletionOptionId"/>.
        /// </summary>
        internal static readonly EditorOptionKey<bool> RemoteControlledResponsiveCompletionOptionId = new EditorOptionKey<bool>(RemoteControlledResponsiveCompletionOptionName);
        internal const string RemoteControlledResponsiveCompletionOptionName = "RemoteControlledResponsiveCompletion";

        /// <summary>
        /// This option is no longer used. Back when it was used,
        /// if set to true, Editor produced a detailed log for a particular scenario of interest.
        /// </summary>
        internal static readonly EditorOptionKey<bool> DiagnosticModeOptionId = new EditorOptionKey<bool>(DiagnosticModeOptionName);
        internal const string DiagnosticModeOptionName = "DiagnosticMode";

        /// <summary>
        /// Determines whether automatic formatting should adapt to the contents of the file instead of user options.
        /// </summary>
        public static readonly EditorOptionKey<bool> AdaptiveFormattingOptionId = new EditorOptionKey<bool>(AdaptiveFormattingOptionName);
        public const string AdaptiveFormattingOptionName = "AdaptiveFormatting";
        #endregion
    }

    #region Option definitions
    /// <summary>
    /// The option definition that holds the raw coding-conventions (editorconfig) snapshot for
    /// the buffer. The host's editorconfig integration populates it; consumers (e.g. Roslyn's
    /// EditorAnalyzerConfigOptions) read it via the untyped option accessor, so the definition
    /// must exist even when no host provides a value.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.RawCodingConventionsSnapshotOptionName)]
    [Shared]
    public sealed class RawCodingConventionsSnapshot : EditorOptionDefinition<System.Collections.Generic.IDictionary<string, object>>
    {
        /// <summary>
        /// Gets the default value (<c>null</c>: no coding conventions available).
        /// </summary>
        public override System.Collections.Generic.IDictionary<string, object> Default { get { return null; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<System.Collections.Generic.IDictionary<string, object>> Key { get { return DefaultOptions.RawCodingConventionsSnapshotOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether to convert tabs to spaces.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.ConvertTabsToSpacesOptionName)]
    [Shared]
    public sealed class ConvertTabsToSpaces : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>true</c>)>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOptions.ConvertTabsToSpacesOptionId; } }
    }

    /// <summary>
    /// The option definition that determines the size (in number of spaces) of a tab.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.TabSizeOptionName)]
    [Shared]
    public sealed class TabSize : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (4).
        /// </summary>
        public override int Default { get { return 4; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOptions.TabSizeOptionId; } }

        /// <summary>
        /// Determines whether a given tab size is valid.
        /// </summary>
        /// <param name="proposedValue">The size of the tab, in number of spaces.</param>
        /// <returns><c>true</c> if <paramref name="proposedValue"/> is a valid size, otherwise <c>false</c>.</returns>
        public override bool IsValid(ref int proposedValue)
        {
            return proposedValue > 0;
        }
    }

    /// <summary>
    /// The option definition that determines the size (in number of spaces) of an indent.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.IndentSizeOptionName)]
    [Shared]
    public sealed class IndentSize : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (4).
        /// </summary>
        public override int Default { get { return 4; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOptions.IndentSizeOptionId; } }

        /// <summary>
        /// Determines whether a given indent size is valid.
        /// </summary>
        /// <param name="proposedValue">The size of the indent, in number of spaces.</param>
        /// <returns><c>true</c> if <paramref name="proposedValue"/> is a valid size, otherwise <c>false</c>.</returns>
        public override bool IsValid(ref int proposedValue)
        {
            return proposedValue > 0;
        }
    }

    /// <summary>
    /// The option definition that determines whether to duplicate a newline character when inserting a line.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.ReplicateNewLineCharacterOptionName)]
    [Shared]
    public sealed class ReplicateNewLineCharacter : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>true</c>).
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOptions.ReplicateNewLineCharacterOptionId; } }
    }

    /// <summary>
    /// The option definition that specifies the newline character or characters.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.NewLineCharacterOptionName)]
    [Shared]
    public sealed class NewLineCharacter : EditorOptionDefinition<string>
    {
        /// <summary>
        /// Gets the default value ("\r\n").
        /// </summary>
        public override string Default { get { return "\r\n"; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<string> Key { get { return DefaultOptions.NewLineCharacterOptionId; } }
    }

    /// <summary>
    /// The option definition that determines the threshold for special handling of long lines.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.LongBufferLineThresholdOptionName)]
    [Shared]
    public sealed class LongBufferLineThreshold : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (32K).
        /// </summary>
        public override int Default { get { return 32 * 1024; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOptions.LongBufferLineThresholdId; } }
    }

    /// <summary>
    /// The option definition that determines the determines the chunking size for long lines.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.LongBufferLineChunkLengthOptionName)]
    [Shared]
    public sealed class LongBufferLineChunk : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (4K).
        /// </summary>
        public override int Default { get { return 4 * 1024; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOptions.LongBufferLineChunkLengthId; } }
    }

    /// <summary>
    /// The option definition that determines whether to trim trailing whitespace.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.TrimTrailingWhiteSpaceOptionName)]
    [Shared]
    public sealed class TrimTrailingWhiteSpace : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>false</c>).
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOptions.TrimTrailingWhiteSpaceOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether to insert a final newline.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.InsertFinalNewLineOptionName)]
    [Shared]
    public sealed class InsertFinalNewLine : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>false</c>).
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOptions.InsertFinalNewLineOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether to insert a final newline.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.TooltipAppearanceCategoryOptionName)]
    [Shared]
    public sealed class TooltipAppearanceCategory : EditorOptionDefinition<string>
    {
        /// <summary>
        /// Gets the default value ("text").
        /// </summary>
        public override string Default { get => "text"; }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<string> Key { get { return DefaultOptions.TooltipAppearanceCategoryOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether files, when opened, attempt to detect for a utf-8 encoding.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.AutoDetectUtf8Name)]
    [Shared]
    public sealed class AutoDetectUtf8Option : EditorOptionDefinition<bool>
    {
        public override bool Default { get => true; }

        public override EditorOptionKey<bool> Key { get { return DefaultOptions.AutoDetectUtf8Id; } }
    }

    /// <summary>
    /// The option definition that determines whether matching delimiters should be highlighted.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.AutomaticDelimiterHighlightingName)]
    [Shared]
    public sealed class AutomaticDelimiterHighlightingOption : EditorOptionDefinition<bool>
    {
        public override bool Default { get => true; }

        public override EditorOptionKey<bool> Key { get { return DefaultOptions.AutomaticDelimiterHighlightingId; } }
    }

    /// <summary>
    /// The option definition that determines whether files should follow project coding conventions.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.FollowCodingConventionsName)]
    [Shared]
    public sealed class FollowCodingConventionsOption : EditorOptionDefinition<bool>
    {
        public override bool Default { get => true; }

        public override EditorOptionKey<bool> Key { get { return DefaultOptions.FollowCodingConventionsId; } }
    }

    /// <summary>
    /// The option definition that determines the editor emulation mode.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.EditorEmulationModeName)]
    [Shared]
    public sealed class EditorEmulationModeOption : EditorOptionDefinition<int>
    {
        public override int Default { get => 0; }

        public override EditorOptionKey<int> Key { get { return DefaultOptions.EditorEmulationModeId; } }
    }

    /// <summary>
    ///The option definition that determines whether to enable typing latency guarding.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.EnableTypingLatencyGuardOptionName)]
    [Shared]
    public sealed class EnableTypingLatencyGuard : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (true).
        /// </summary>
        public override bool Default { get => true; }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultOptions.EnableTypingLatencyGuardOptionId; } }
    }

    // The option definition for DefaultOptions.FallbackFontId is in the implementation DLLs (since the name of the default fallback will depend
    // on the rendering system).

    /// <summary>
    /// The option definition that determines maximum allowed typing latency value in milliseconds. Its value comes either
    /// from remote settings or from <see cref="UserCustomMaximumTypingLatencyOption"/> if user specifies it in
    /// Tools/Options/Text Editor/Advanced page.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.MaximumTypingLatencyOptionName)]
    [Shared]
    public sealed class MaximumTypingLatency : EditorOptionDefinition<int>
    {
        /// <summary>
        /// Gets the default value (infinite).
        /// </summary>
        public override int Default { get => Timeout.Infinite; }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<int> Key { get { return DefaultOptions.MaximumTypingLatencyOptionId; } }
    }

    /// <summary>
    /// The option definition that determines user custom maximum allowed typing latency value in milliseconds. If user
    /// specifies it on Tools/Options/Text Editor/Advanced page, it becomes a source for the <see cref="MaximumTypingLatency"/> option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.UserCustomMaximumTypingLatencyOptionName)]
    [Shared]
    public sealed class UserCustomMaximumTypingLatencyOption : EditorOptionDefinition<int>
    {
        public override int Default { get { return Timeout.Infinite; } }
        public override EditorOptionKey<int> Key { get { return DefaultOptions.UserCustomMaximumTypingLatencyOptionId; } }
    }

    /// <summary>
    /// The option definition that determines whether editor uses non blocking completion mode,
    /// where editor does not wait for completion items to arrive when user presses a commit character.
    /// This option is not exposed to the users. It is controllable by laguage services.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.NonBlockingCompletionOptionName)]
    [Shared]
    public sealed class NonBlockingCompletionOption : EditorOptionDefinition<bool>
    {
        public override bool Default => false;
        public override EditorOptionKey<bool> Key => DefaultOptions.NonBlockingCompletionOptionId;
    }

    /// <summary>
    /// The option definition that determines whether editor uses responsive completion  mode,
    /// where editor waits short amount of time for completion items when user presses a commit character.
    /// If completion items still don't exist after the delay, completion is dismissed.
    /// This option is exposed to the users at Tools/Options/Text Editor/Advanced page.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.ResponsiveCompletionOptionName)]
    [Shared]
    public sealed class ResponsiveCompletionOption : EditorOptionDefinition<bool>
    {
        public override bool Default => true;
        public override EditorOptionKey<bool> Key => DefaultOptions.ResponsiveCompletionOptionId;
    }

    /// <summary>
    /// The option definition that determines the maximum allowed delay in responsive completion mode,
    /// where editor waits specified amount of time for completion items when user presses a commit character.
    /// If completion items still don't exist after the delay, completion is dismissed.
    /// This option is not exposed to the users. It is controllable by remote setting.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.ResponsiveCompletionThresholdOptionName)]
    [Shared]
    public sealed class ResponsiveCompletionThresholdOption : EditorOptionDefinition<int>
    {
        public override int Default => 250;
        public override EditorOptionKey<int> Key => DefaultOptions.ResponsiveCompletionThresholdOptionId;
    }

    /// <summary>
    /// The option definition that determines whether responsive mode should be disabled.
    /// This option is set using remotely controllable feature flag, and is set to <c>true</c>
    /// so that responsive mode remains enabled when feature flag service may not be reached.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.RemoteControlledResponsiveCompletionOptionName)]
    [Shared]
    public sealed class RemoteControlledResponsiveCompletionOption : EditorOptionDefinition<bool>
    {
        public override bool Default => true;
        public override EditorOptionKey<bool> Key => DefaultOptions.RemoteControlledResponsiveCompletionOptionId;
    }

    /// <summary>
    /// This option is no longer used
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.DiagnosticModeOptionName)]
    [Shared]
    public sealed class DiagnosticModeOption : EditorOptionDefinition<bool>
    {
        public override bool Default => false;
        public override EditorOptionKey<bool> Key => DefaultOptions.DiagnosticModeOptionId;
    }

    /// <summary>
    /// Determines whether automatic formatting should adapt to the contents of the file instead of user options.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultOptions.AdaptiveFormattingOptionName)]
    [Shared]
    public sealed class AdaptiveFormattingOption : EditorOptionDefinition<bool>
    {
        public override bool Default => true;
        public override EditorOptionKey<bool> Key => DefaultOptions.AdaptiveFormattingOptionId;
    }

    #endregion
}
