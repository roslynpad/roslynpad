//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Editor;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Text.Document;

    [Export(typeof(ITextDocumentFactoryService))]
    [Shared]
    public sealed partial class TextDocumentFactoryService : ITextDocumentFactoryService
    {
        #region Internal Consumptions
        
        [Import]
        public ITextBufferFactoryService BufferFactoryService { get; set; }

        [ImportMany]
        public Lazy<IEncodingDetector, EncodingDetectorMetadata>[] UnorderedEncodingDetectors { get; set; }

        [Import]
        public GuardedOperations GuardedOperations { get; set; }

        [Import]
        public IWhitespaceManagerFactory WhitespaceManagerFactory { get; set; }

        #endregion

        internal static Encoding DefaultEncoding = Encoding.Default; // Exposed for unit tests.
        static Encoding UTF8WithoutBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        #region ITextDocumentFactoryService Members

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType)
        {
            bool unused;
            return CreateAndLoadTextDocument(filePath, contentType, attemptUtf8Detection: true, characterSubstitutionsOccurred: out unused);
        }

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, Encoding encoding, out bool characterSubstitutionsOccurred)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var fallbackDetector = new FallbackDetector(encoding.DecoderFallback);
            var modifiedEncoding = (Encoding)encoding.Clone();
            modifiedEncoding.DecoderFallback = fallbackDetector;

            ITextBuffer buffer;
            DateTime lastModified;
            long fileSize;
            using (Stream stream = OpenFile(filePath, out lastModified, out fileSize))
            {
                // Caller knows best, so don't use byte order marks.
                using (StreamReader reader = new StreamReader(stream, modifiedEncoding, detectEncodingFromByteOrderMarks: false))
                {
                    System.Diagnostics.Debug.Assert(encoding.CodePage == reader.CurrentEncoding.CodePage);
                    buffer = ((ITextBufferFactoryService2)BufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                }
            }

            characterSubstitutionsOccurred = fallbackDetector.FallbackOccurred;

#if _DEBUG
            TextUtilities.TagBuffer(buffer, filePath);
#endif
            TextDocument textDocument = new TextDocument(buffer, filePath, lastModified, this, encoding, explicitEncoding: true);

            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public ITextDocument CreateAndLoadTextDocument(string filePath, IContentType contentType, bool attemptUtf8Detection, out bool characterSubstitutionsOccurred)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            characterSubstitutionsOccurred = false;

            Encoding chosenEncoding = null;
            ITextBuffer buffer = null;
            DateTime lastModified;
            long fileSize;

            // select matching detectors without instantiating any
            var detectors = ExtensionSelector.SelectMatchingExtensions(OrderedEncodingDetectors, contentType);

            using (Stream stream = OpenFile(filePath, out lastModified, out fileSize))
            {
                // First, look for a byte order marker and let the encoding detecters
                // suggest encodings.
                chosenEncoding = EncodedStreamReader.DetectEncoding(stream, detectors, GuardedOperations);

                // If that didn't produce a result, tentatively try to open as UTF 8.
                if (chosenEncoding == null && attemptUtf8Detection)
                {
                    try
                    {
                        var detectorEncoding = new ExtendedCharacterDetector();

                        using (StreamReader reader = new EncodedStreamReader.NonStreamClosingStreamReader(stream, detectorEncoding, false))
                        {
                            buffer = ((ITextBufferFactoryService2)BufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                            characterSubstitutionsOccurred = false;
                        }

                        if (detectorEncoding.DecodedExtendedCharacters)
                        {
                            // Valid UTF-8 but has bytes that are not merely ASCII.
                            chosenEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                        }
                        else
                        {
                            // Valid UTF8 but no extended characters, so it's valid ASCII.
                            // We don't use ASCII here because of the following scenario:
                            // The user with a non-ENU system encoding opens a code file with ASCII-only contents
                            if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
                                chosenEncoding = DefaultEncoding;
                            else
                                // To get to this line, it means file doesn't have BOM
                                // On Windows DefaultEncoding is "ASCII" which doesn't have BOM
                                // On non-Windows systems DefaultEncoding is UTF8 which emits BOM on save
                                // which is something we don't want(on file that didn't have BOM to save BOM)
                                // So instead we use "new UTF8Encoding (encoderShouldEmitUTF8Identifier: false)", which means don't emit BOM when saving.
                                chosenEncoding = UTF8WithoutBOM;
                        }
                    }
                    catch (DecoderFallbackException)
                    {
                        // Not valid UTF-8.
                        // Proceed to the next if block to try the system's default codepage.
                        Debug.Assert(buffer == null);
                        buffer = null;
                        stream.Position = 0;
                    }
                }

                Debug.Assert(buffer == null || chosenEncoding != null);

                // If all else didn't work, use system's default encoding.
                if (chosenEncoding == null)
                {
                    chosenEncoding = DefaultEncoding;
                }

                if (buffer == null)
                {
                    var fallbackDetector = new FallbackDetector(chosenEncoding.DecoderFallback);
                    var modifiedEncoding = (Encoding)chosenEncoding.Clone();
                    modifiedEncoding.DecoderFallback = fallbackDetector;

                    Debug.Assert(stream.Position == 0);

                    using (StreamReader reader = new EncodedStreamReader.NonStreamClosingStreamReader(stream, modifiedEncoding, detectEncodingFromByteOrderMarks: false))
                    {
                        Debug.Assert(chosenEncoding.CodePage == reader.CurrentEncoding.CodePage);
                        buffer = ((ITextBufferFactoryService2)BufferFactoryService).CreateTextBuffer(reader, contentType, fileSize, filePath);
                    }

                    characterSubstitutionsOccurred = fallbackDetector.FallbackOccurred;
                }
            }

            TextDocument textDocument = new TextDocument(buffer, filePath, lastModified, this, chosenEncoding, attemptUtf8Detection: attemptUtf8Detection);

            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public ITextDocument CreateTextDocument(ITextBuffer textBuffer, string filePath)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            TextDocument textDocument = new TextDocument(textBuffer, filePath, DateTime.UtcNow, this, Encoding.UTF8);
            RaiseTextDocumentCreated(textDocument);

            return textDocument;
        }

        public bool TryGetTextDocument(ITextBuffer textBuffer, out ITextDocument textDocument)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            textDocument = null;

            TextDocument document;
            if (textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document))
            {
                if(document != null && !document.IsDisposed)
                {
                    textDocument = document;
                    return true;
                }
                else
                {
                    Debug.Fail("There shouldn't be a null or disposed document in the buffer's property bag.  Did someone else put it there?");
                }
            }

            return false;
        }

        public event EventHandler<TextDocumentEventArgs> TextDocumentCreated;

        public event EventHandler<TextDocumentEventArgs> TextDocumentDisposed;

        #endregion

        #region helpers

        /// <summary>
        /// Helper method to raise the <see cref="ITextDocumentFactoryService.TextDocumentCreated"/> event.
        /// </summary>
        /// <param name="textDocument">The <see cref="ITextDocument"/> that was created.</param>
        private void RaiseTextDocumentCreated(ITextDocument textDocument)
        {
            EventHandler<TextDocumentEventArgs> documentCreated = this.TextDocumentCreated;
            if (documentCreated != null)
            {
                documentCreated.Invoke(this, new TextDocumentEventArgs(textDocument));
            }
        }

        /// <summary>
        /// Helper method to raise the <see cref="ITextDocumentFactoryService.TextDocumentDisposed"/> event.
        /// </summary>
        /// <param name="textDocument">The <see cref="ITextDocument"/> that was disposed.</param>
        internal void RaiseTextDocumentDisposed(ITextDocument textDocument)
        {
            EventHandler<TextDocumentEventArgs> documentDisposed = this.TextDocumentDisposed;
            if (documentDisposed != null)
            {
                documentDisposed.Invoke(this, new TextDocumentEventArgs(textDocument));
            }
        }

        private IList<Lazy<IEncodingDetector, EncodingDetectorMetadata>> _orderedEncodingDetectors;

        internal IEnumerable<Lazy<IEncodingDetector, EncodingDetectorMetadata>> OrderedEncodingDetectors
        {
            get
            {
                if (_orderedEncodingDetectors == null)
                {
                    if (UnorderedEncodingDetectors != null)
                    {
                        _orderedEncodingDetectors = Orderer.Order(UnorderedEncodingDetectors);
                    }
                    else
                    {
                        _orderedEncodingDetectors = new List<Lazy<IEncodingDetector, EncodingDetectorMetadata>>();
                    }
                }
                return _orderedEncodingDetectors;
            }
            set // for unit test helper.
            {
                _orderedEncodingDetectors = new List<Lazy<IEncodingDetector, EncodingDetectorMetadata>>(value);
            }
        }

        // Exposed for testing.
        internal Func<string, Stream> StreamCreator;

        private Stream OpenFile(string filePath, out DateTime lastModifiedTimeUtc, out long fileSize)
        {
            if (StreamCreator != null)
            {
                lastModifiedTimeUtc = DateTime.UtcNow;
                fileSize = -1;  // a signal that the file size is not known
                return StreamCreator(filePath);
            }
            else
            {
                return OpenFileGuts(filePath, out lastModifiedTimeUtc, out fileSize);
            }
        }

        internal static Stream OpenFileGuts(string filePath, out DateTime lastModifiedTimeUtc, out long fileSize)
        {
            // Sometimes files are held open with FILE_FLAG_DELETE_ON_CLOSE before the editor
            // is asked to open them. We should support that by allowing FileShare.Delete.
            Stream result = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            FileInfo fileInfo = new FileInfo(filePath);
            lastModifiedTimeUtc = fileInfo.LastWriteTimeUtc;
            fileSize = fileInfo.Length;
            if (fileSize > int.MaxValue)
            {
                throw new InvalidOperationException(Strings.FileTooLarge);
            }

            return result;
        }

        // For unit testing purposes
        internal void Initialize(ITextBufferFactoryService bufferFactoryService)
        {
            Initialize(bufferFactoryService, null);
        }

        internal void Initialize(ITextBufferFactoryService bufferFactoryService, List<Lazy<IEncodingDetector, EncodingDetectorMetadata>> detectors)
        {
            BufferFactoryService = bufferFactoryService;
            UnorderedEncodingDetectors = detectors?.ToArray() ?? Array.Empty<Lazy<IEncodingDetector, EncodingDetectorMetadata>>();
        }

        #endregion
    }
}
