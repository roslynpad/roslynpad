//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal static class TextImageLoader
    {
        public const int BlockSize = 16384;

        internal static StringRebuilder Load(TextReader reader, long fileSize,
                                             out NewlineState newlineState,
                                             out LeadingWhitespaceState leadingWhitespaceState,
                                             out int longestLineLength,
                                             int blockSize = 0,
                                             int minCompressedBlockSize = TextImageLoader.BlockSize,                                             // Exposed for unit tests
                                             bool throwOnInvalidCharacters = false)
        {
            newlineState = new NewlineState();
            leadingWhitespaceState = new LeadingWhitespaceState();

            int currentLineLength = 0;
            longestLineLength = 0;
            char thresholdForInvalidCharacters = throwOnInvalidCharacters ? '\u0001' : '\0';    // Basically the only invalid character is \0, if we are looking for invalid characters.

            bool useCompressedStringRebuilders = (fileSize >= TextModelOptions.CompressedStorageFileSizeThreshold);
            if (blockSize == 0)
                blockSize = useCompressedStringRebuilders ? TextModelOptions.CompressedStoragePageSize : TextImageLoader.BlockSize;

            PageManager pageManager = null;
            char[] buffer;
            if (useCompressedStringRebuilders)
            {
                pageManager = new PageManager();
                buffer = new char[blockSize];
            }
            else
            {
                buffer = TextImageLoader.AcquireBuffer(blockSize);
            }

            StringRebuilder content = StringRebuilderForChars.Empty;
            try
            {
                bool nextCharIsStartOfLine = true;
                while (true)
                {
                    int read = TextImageLoader.LoadNextBlock(reader, buffer);

                    if (read == 0)
                        break;

                    var lineBreaks = TextImageLoader.ParseBlock(
                        buffer,
                        read,
                        thresholdForInvalidCharacters,
                        ref newlineState,
                        ref leadingWhitespaceState,
                        ref currentLineLength,
                        ref longestLineLength,
                        ref nextCharIsStartOfLine);

                    char[] bufferForStringBuilder = buffer;
                    if (read < (buffer.Length / 2))
                    {
                        // We read far less characters than buffer so copy the contents to a new buffer and reuse the original buffer.
                        bufferForStringBuilder = new char[read];
                        Array.Copy(buffer, bufferForStringBuilder, read);
                    }
                    else
                    {
                        // We're using most of buffer so allocate a new block for the next chunk.
                        buffer = new char[blockSize];
                    }

                    var newContent = (useCompressedStringRebuilders && (read > minCompressedBlockSize))
                                     ? StringRebuilderForCompressedChars.Create(new Page(pageManager, bufferForStringBuilder, read), lineBreaks)
                                     : StringRebuilderForChars.Create(bufferForStringBuilder, read, lineBreaks);

                    content = content.Insert(content.Length, newContent);
                }

                longestLineLength = Math.Max(longestLineLength, currentLineLength);
            }
            finally
            {
                if (!useCompressedStringRebuilders)
                {
                    TextImageLoader.ReleaseBuffer(buffer);
                }
            }

            return content;
        }

        public static int LoadNextBlock(TextReader reader, char[] buffer)
        {
            // Reserve 1 spot for a potential CR at the end of the buffer (in which we want to add the next LF, if it exists)
            int read = reader.ReadBlock(buffer, 0, buffer.Length - 1);
            if ((read == buffer.Length - 1) && (buffer[read - 1] == '\r'))
            {
                // Last character read was a CR and there is, probably since we read the entire block, more to go.
                var next = reader.Peek();
                if (next == '\n')
                {
                    // We had a crlf that spanned the end of the buffer. Add it to the buffer and carry on.
                    // In theory we could append anything other than another CR but having the block end at
                    // the end of a line is a good thing.
                    reader.Read();
                    buffer[read++] = '\n';
                }
            }

            return read;
        }

        // Evil performance hack (but we are on a hot path here):
        //  thresholdForInvalidCharacters should be '\u0001' if we are throwing on invalid characters.
        //                                should be '\0' if we are not.
        // (otherwise we need to check both a throwOnInvalidCharacters boolean and that c == 0).
        private static ILineBreaks ParseBlock(char[] buffer, int length, char thresholdForInvalidCharacters,
                                              ref NewlineState newlineState,
                                              ref LeadingWhitespaceState leadingWhitespaceState,
                                              ref int currentLineLength,
                                              ref int longestLineLength,
                                              ref bool nextCharIsStartOfLine)
        {
            // Note that the lineBreaks created here will (internally) use the pooled list of line breaks.
            IPooledLineBreaksEditor lineBreaks = LineBreakManager.CreatePooledLineBreakEditor(length);

            int index = 0;
            while (index < length)
            {
                int breakLength = TextUtilities.LengthOfLineBreak(buffer, index, length);
                if (breakLength == 0)
                {
                    char c = buffer[index];

                    // If we are checking for invalid characters, throw if we encounter a \0
                    if (c < thresholdForInvalidCharacters)
                        throw new InvalidDataException("File contains NUL characters");

                    ++currentLineLength;
                    ++index;

                    if (nextCharIsStartOfLine)
                    {
                        switch (c)
                        {
                            case ' ':
                                leadingWhitespaceState.Increment(LeadingWhitespaceState.LineLeadingCharacter.Space, 1);
                                break;
                            case '\t':
                                leadingWhitespaceState.Increment(LeadingWhitespaceState.LineLeadingCharacter.Tab, 1);
                                break;
                            default:
                                leadingWhitespaceState.Increment(LeadingWhitespaceState.LineLeadingCharacter.Printable, 1);
                                break;
                        }

                        nextCharIsStartOfLine = false;
                    }
                }
                else
                {
                    lineBreaks.Add(index, breakLength);
                    longestLineLength = Math.Max(longestLineLength, currentLineLength);
                    currentLineLength = 0;


                    if (breakLength == 2)
                    {
                        newlineState.Increment(NewlineState.LineEnding.CRLF, 1);
                    }
                    else
                    {
                        switch (buffer[index])
                        {
                            // This code needs to be kep consistent with TextUtilities.LengthOfLineBreak()
                            case '\r': newlineState.Increment(NewlineState.LineEnding.CR, 1); break;
                            case '\n': newlineState.Increment(NewlineState.LineEnding.LF, 1); break;
                            case '\u0085': newlineState.Increment(NewlineState.LineEnding.NEL, 1); break;
                            case '\u2028': newlineState.Increment(NewlineState.LineEnding.LS, 1); break;
                            case '\u2029': newlineState.Increment(NewlineState.LineEnding.PS, 1); break;
                            default: throw new InvalidOperationException("Unexpected line ending");
                        }
                    }

                    if (nextCharIsStartOfLine)
                    {
                        leadingWhitespaceState.Increment(LeadingWhitespaceState.LineLeadingCharacter.Empty, 1);
                    }

                    nextCharIsStartOfLine = true;
                }

                index += breakLength;
            }

            lineBreaks.ReleasePooledLineBreaks();

            return lineBreaks;
        }

        private static char[] pooledBuffer;

        private static char[] AcquireBuffer(int size)
        {
            char[] buffer = Volatile.Read(ref pooledBuffer);
            if (buffer != null && buffer.Length >= size)
            {
                if (buffer == Interlocked.CompareExchange(ref pooledBuffer, null, buffer))
                {
                    return buffer;
                }
            }

            return new char[size];
        }

        private static void ReleaseBuffer(char[] buffer)
        {
            Interlocked.CompareExchange(ref pooledBuffer, buffer, null);
        }
    }
}
