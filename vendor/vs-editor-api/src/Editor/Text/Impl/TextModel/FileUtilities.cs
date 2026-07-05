//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class FileUtilities
    {
        public static void SaveSnapshot(ITextSnapshot snapshot,
                                        FileMode fileMode,
                                        Encoding encoding,
                                        string filePath)
        {
            Debug.Assert((fileMode == FileMode.Create) || (fileMode == FileMode.CreateNew));

            //Save the contents of the text buffer to disk.

            string temporaryFilePath = null;
            try
            {
                FileStream originalFileStream = null;
                FileStream temporaryFileStream = FileUtilities.CreateFileStream(filePath, fileMode, out temporaryFilePath, out originalFileStream);
                if (originalFileStream == null)
                {
                    //The "normal" scenario: save the snapshot directly to disk. Either:
                    // there are no hard links to the target file so we can write the snapshot to the temporary and use File.Replace.
                    // we're creating a new file (in which case, temporaryFileStream is a misnomer: it is the stream for the file we are creating).
                    try
                    {
                        using (StreamWriter streamWriter = new StreamWriter(temporaryFileStream, encoding))
                        {
                            snapshot.Write(streamWriter);
                        }
                    }
                    finally
                    {
                        //This is somewhat redundant: disposing of streamWriter had the side-effect of disposing of temporaryFileStream
                        temporaryFileStream.Dispose();
                        temporaryFileStream = null;
                    }

                    if (temporaryFilePath != null)
                    {
                        //We were saving to the original file and already have a copy of the file on disk.
                        int remainingAttempts = 3;
                        do
                        {
                            try
                            {
                                //Replace the contents of filePath with the contents of the temporary using File.Replace to
                                //preserve the various attributes of the original file.
                                File.Replace(temporaryFilePath, filePath, null, true);
                                temporaryFilePath = null;

                                return;
                            }
                            catch (FileNotFoundException)
                            {
                                // The target file doesn't exist (someone deleted it after we detected it earlier).
                                // This is an acceptable condition so don't throw.
                                File.Move(temporaryFilePath, filePath);
                                temporaryFilePath = null;

                                return;
                            }
                            catch (IOException)
                            {
                                //There was some other exception when trying to replace the contents of the file
                                //(probably because some other process had the file locked).
                                //Wait a few ms and try again.
                                System.Threading.Thread.Sleep(5);
                            }
                        }
                        while (--remainingAttempts > 0);

                        //We're giving up on replacing the file. Try overwriting it directly (this is essentially the old Dev11 behavior).
                        //Do not try approach we are using for hard links (copying the original & restoring it if there is a failure) since
                        //getting here implies something strange is going on with the file system (Git or the like locking files) so we
                        //want the simplest possible fallback.

                        //Failing here causes the exception to be passed to the calling code.
                        using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(stream, encoding))
                            {
                                snapshot.Write(streamWriter);
                            }
                        }
                    }
                }
                else
                {
                    //filePath has hard links so we need to use a different approach to save the file:
                    // copy the original file to the temporary
                    // write directly to the original
                    // restore the original in the event of errors (which could be encoding errors and not disk issues) if there's a problem.
                    try
                    {
                        // Copy the contents of the original file to the temporary.
                        originalFileStream.CopyTo(temporaryFileStream);

                        //We've got a clean copy, try writing the snapshot directly to the original file
                        try
                        {
                            originalFileStream.Seek(0, SeekOrigin.Begin);
                            originalFileStream.SetLength(0);

                            //Make sure the StreamWriter is flagged leaveOpen == true. Otherwise disposing of the StreamWriter will dispose of originalFileStream and we need to
                            //leave originalFileStream open so we can use it to restore the original from the temporary copy we made.
                            using (var streamWriter = new StreamWriter(originalFileStream, encoding, bufferSize: 1024, leaveOpen: true))        //1024 == the default buffer size for a StreamWriter.
                            {
                                snapshot.Write(streamWriter);
                            }
                        }
                        catch
                        {
                            //Restore the original from the temporary copy we made (but rethrow the original exception since we didn't save the file).
                            temporaryFileStream.Seek(0, SeekOrigin.Begin);

                            originalFileStream.Seek(0, SeekOrigin.Begin);
                            originalFileStream.SetLength(0);

                            temporaryFileStream.CopyTo(originalFileStream);

                            throw;
                        }
                    }
                    finally
                    {
                        originalFileStream.Dispose();
                        originalFileStream = null;

                        temporaryFileStream.Dispose();
                        temporaryFileStream = null;
                    }
                }
            }
            finally
            {
                if (temporaryFilePath != null)
                {
                    try
                    {
                        //We do not need the temporary any longer.
                        if (File.Exists(temporaryFilePath))
                        {
                            File.Delete(temporaryFilePath);
                        }
                    }
                    catch
                    {
                        //Failing to clean up the temporary is an ignorable exception.
                    }
                }
            }
        }

        private static FileStream CreateFileStream(string filePath, FileMode fileMode, out string temporaryPath, out FileStream originalFileStream)
        {
            originalFileStream = null;

            if (File.Exists(filePath))
            {
                // We're writing to a file that already exists. This is an error if we're trying to do a CreateNew.
                if (fileMode == FileMode.CreateNew)
                {
                    throw new IOException(filePath + " exists");
                }

                try
                {
                    originalFileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    //Even though SafeFileHandle is an IDisposable, we don't dispose of it since that closes the stream.
                    var safeHandle = originalFileStream.SafeFileHandle;
                    if (!(safeHandle.IsClosed || safeHandle.IsInvalid))
                    {
                        uint numberOfHardLinks = 1;
                        bool statWasSuccessful = false;

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            if (NativeMethods.GetFileInformationByHandle(safeHandle, out var fi))
                            {
                                statWasSuccessful = true;
                                numberOfHardLinks = fi.NumberOfLinks;
                            }
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            if (NativeMethods.DarwinStat((int)safeHandle.DangerousGetHandle(), out var statbuf) == 0)
                            {
                                statWasSuccessful = true;
                                numberOfHardLinks = statbuf.st_nlink;
                            }
                        }
                        else
                        {
                            throw new PlatformNotSupportedException("Implement fstat support for Linux");
                        }

                        if (!statWasSuccessful)
                            throw new InvalidOperationException("Unable to determine if file has any hard links");

                        if (numberOfHardLinks <= 1)
                        {
                            // The file we're trying to write to doesn't have any hard links...
                            // clear out the originalFileStream as a clue.
                            originalFileStream.Dispose();
                            originalFileStream = null;
                        }
                    }
                }
                catch
                {
                    if (originalFileStream != null)
                    {
                        originalFileStream.Dispose();
                        originalFileStream = null;
                    }

                    //We were not able to determine whether or not the file had hard links so throw here (aborting the save)
                    //since we don't know how to do it safely.
                    throw;
                }

                string root = Path.GetDirectoryName(filePath);

                int count = 0;
                while (++count < 20)
                {
                    try
                    {
                        temporaryPath = Path.Combine(root, Path.GetRandomFileName() + "~");   //The ~ suffix hides the temporary file from GIT.
                        return new FileStream(temporaryPath, FileMode.CreateNew, (originalFileStream != null) ? FileAccess.ReadWrite : FileAccess.Write, FileShare.None);
                    }
                    catch (IOException)
                    {
                        //Ignore IOExceptions ... GetRandomFileName() came up with a duplicate so we need to try again.
                    }
                }

                Debug.Fail("Unable to create a temporary file");
            }

            temporaryPath = null;
            return new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read);
        }
    }
}
