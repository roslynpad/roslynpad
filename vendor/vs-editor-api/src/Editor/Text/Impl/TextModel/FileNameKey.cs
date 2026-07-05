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
    using System.IO;
    sealed class FileNameKey
    {
        private readonly string _fileName;
        private readonly int _hashCode;

        public FileNameKey(string fileName)
        {
            //Gracefully catch errors getting the full path (which can happen if the file name is on a protected share).
            try
            {
                _fileName = Path.GetFullPath(fileName);
            }
            catch
            {
                //This shouldn't happen (we are generally passed names associated with documents that we are expecting to open so
                //we should have access). If we fail, we will, at worst not get the same underlying document when people create
                //persistent spans using unnormalized names.
                _fileName = fileName;
            }

            _hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_fileName);
        }

        //Override equality and hash code
        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FileNameKey;
            return (other != null) && string.Equals(_fileName, other._fileName, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return _fileName;
        }
    }
}
