using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Identifies a filter that toggles exclusive display of <see cref="CompletionItem"/>s that reference it.
    /// </summary>
    /// <remarks>
    /// These instances should be singletons. All <see cref="CompletionItem"/>s that should be filtered
    /// using the same filter button must use the same reference to the instance of <see cref="CompletionFilter"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// static CompletionFilter MyFilter = new CompletionFilter("My items", "m", MyItemsImageElement);
    /// </code>
    /// </example>
    [DebuggerDisplay("{DisplayText}")]
    public class CompletionFilter : INotifyPropertyChanged
    {
        /// <summary>
        /// Localized name of this filter.
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// Key used in a keyboard shortcut that toggles this filter.
        /// </summary>
        public string AccessKey { get; }

        /// <summary>
        /// <see cref="ImageElement"/> that represents this filter.
        /// </summary>
        public ImageElement Image { get; }

        /// <summary>
        /// Constructs an instance of <see cref="CompletionFilter"/>.
        /// </summary>
        /// <param name="displayText">Name of this filter</param>
        /// <param name="accessKey">Key used in a keyboard shortcut that toggles this filter.</param>
        /// <param name="image">Image which represents this filter</param>
        public CompletionFilter(string displayText, string accessKey, ImageElement image)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Display text must be non-empty", nameof(displayText));
            }
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new ArgumentException("Access key must be non-empty", nameof(accessKey));
            }

            DisplayText = displayText;
            AccessKey = accessKey;
            Image = image;
        }

        #region INotifyPropertyChanged members

        // The properties of this class don't change,
        // we're just implementing INotifyPropertyChanged to prevent WPF leaking memory.
        #pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
        #pragma warning restore CS0067

        #endregion
    }
}
